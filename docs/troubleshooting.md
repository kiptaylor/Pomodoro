# Troubleshooting

## “I can’t see the tray icon”

- Make sure the app is running (check Task Manager).
- Windows sometimes hides tray icons behind the “^” overflow menu.

## “Notifications don’t show”

- Check Windows notification settings for the app.
- Focus Assist / Do Not Disturb can suppress notifications.

## “I can’t overwrite Pomodoro.exe”

If you’re updating the EXE while it’s running:
- Exit the app from the tray menu (or close → choose Exit)
- Then replace the file

## “CLI commands do nothing”

The CLI controls a running instance via local IPC (named pipe).
- Start the app first (double-click).
- Then run `pom open` to verify it can talk to the running instance.

