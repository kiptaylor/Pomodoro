namespace PomodoroCore;

public sealed record PomodoroConfig(
    int WorkMinutes = 25,
    int BreakMinutes = 5,
    int LongBreakMinutes = 15,
    int Cycles = 4,
    bool AutoAdvance = true,
    bool Popup = true,
    bool Sound = true);

public sealed record SessionOptions(
    int WorkSeconds,
    int BreakSeconds,
    int LongBreakSeconds,
    int Cycles,
    bool AutoAdvance,
    bool Popup,
    bool Sound);

public enum PomodoroPhase
{
    Work,
    Break,
    LongBreak
}

public sealed record PomodoroState(
    Guid SessionId,
    PomodoroPhase Phase,
    int CycleIndex,
    int Cycles,
    int WorkSeconds,
    int BreakSeconds,
    int LongBreakSeconds,
    bool AutoAdvance,
    bool Popup,
    bool Sound,
    DateTimeOffset PhaseStartedAtUtc,
    int PhaseDurationSeconds,
    bool IsPaused,
    DateTimeOffset? PausedAtUtc,
    int? PausedRemainingSeconds)
{
    public static PomodoroState New(SessionOptions options, DateTimeOffset nowUtc)
    {
        return new PomodoroState(
            SessionId: Guid.NewGuid(),
            Phase: PomodoroPhase.Work,
            CycleIndex: 1,
            Cycles: options.Cycles,
            WorkSeconds: options.WorkSeconds,
            BreakSeconds: options.BreakSeconds,
            LongBreakSeconds: options.LongBreakSeconds,
            AutoAdvance: options.AutoAdvance,
            Popup: options.Popup,
            Sound: options.Sound,
            PhaseStartedAtUtc: nowUtc,
            PhaseDurationSeconds: options.WorkSeconds,
            IsPaused: false,
            PausedAtUtc: null,
            PausedRemainingSeconds: null);
    }

    public int GetRemainingSeconds(DateTimeOffset nowUtc)
    {
        if (IsPaused && PausedRemainingSeconds is not null) return PausedRemainingSeconds.Value;
        var elapsed = (int)Math.Floor((nowUtc - PhaseStartedAtUtc).TotalSeconds);
        return PhaseDurationSeconds - elapsed;
    }

    public DateTimeOffset GetEndsAtUtc() => PhaseStartedAtUtc.AddSeconds(PhaseDurationSeconds);

    public PomodoroState Pause(DateTimeOffset nowUtc)
    {
        if (IsPaused) return this;
        var remaining = Math.Max(0, GetRemainingSeconds(nowUtc));
        return this with
        {
            IsPaused = true,
            PausedAtUtc = nowUtc,
            PausedRemainingSeconds = remaining
        };
    }

    public PomodoroState Resume(DateTimeOffset nowUtc)
    {
        if (!IsPaused) return this;
        var remaining = PausedRemainingSeconds ?? Math.Max(0, GetRemainingSeconds(nowUtc));
        return this with
        {
            IsPaused = false,
            PausedAtUtc = null,
            PausedRemainingSeconds = null,
            PhaseStartedAtUtc = nowUtc,
            PhaseDurationSeconds = remaining
        };
    }

    public PomodoroState? GetNextPhase(DateTimeOffset nextPhaseStartUtc)
    {
        return Phase switch
        {
            PomodoroPhase.Work => CycleIndex >= Cycles
                ? this with
                {
                    Phase = PomodoroPhase.LongBreak,
                    PhaseStartedAtUtc = nextPhaseStartUtc,
                    PhaseDurationSeconds = LongBreakSeconds,
                    IsPaused = false,
                    PausedAtUtc = null,
                    PausedRemainingSeconds = null
                }
                : this with
                {
                    Phase = PomodoroPhase.Break,
                    PhaseStartedAtUtc = nextPhaseStartUtc,
                    PhaseDurationSeconds = BreakSeconds,
                    IsPaused = false,
                    PausedAtUtc = null,
                    PausedRemainingSeconds = null
                },

            PomodoroPhase.Break => this with
            {
                Phase = PomodoroPhase.Work,
                CycleIndex = CycleIndex + 1,
                PhaseStartedAtUtc = nextPhaseStartUtc,
                PhaseDurationSeconds = WorkSeconds,
                IsPaused = false,
                PausedAtUtc = null,
                PausedRemainingSeconds = null
            },

            PomodoroPhase.LongBreak => null,
            _ => null
        };
    }

    public AdvanceResult AdvanceTo(DateTimeOffset nowUtc)
    {
        if (IsPaused) return new AdvanceResult(this, 0, false);

        var current = this;
        var advanced = 0;

        while (!current.IsPaused && current.GetRemainingSeconds(nowUtc) <= 0)
        {
            var phaseEndedAt = current.GetEndsAtUtc();
            var next = current.GetNextPhase(phaseEndedAt);

            advanced++;

            if (next is null)
            {
                return new AdvanceResult(null, advanced, true);
            }

            current = next;
        }

        return new AdvanceResult(current, advanced, false);
    }
}

public sealed record AdvanceResult(PomodoroState? State, int PhasesAdvanced, bool Completed);

public sealed record LogEvent(string Type, DateTimeOffset AtUtc, PomodoroState State);

