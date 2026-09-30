# WatchCircle 1.0.9.0 — Buttons in der neuen Jellyfin-Kopfzeile

Behebt den fehlenden WatchCircle-Personenbutton und den fehlenden Gemeinsam-schauen-Button in Jellyfins neuer Weboberfläche.

Die neue Oberfläche behält eine alte Kopfzeile unsichtbar im Hintergrund. WatchCircle 1.0.8.0 fügte die Buttons dort ein, sodass sie trotz aktivem Plugin nicht sichtbar waren. Version 1.0.9.0 erkennt die sichtbare neue Kopfzeile und fügt die Buttons vor Jellyfins SyncPlay-/Cast-/Suchbuttons ein.

- Die klassische Kopfzeile wird weiterhin unterstützt.
- Versteckte Kopfzeilen erhalten keine Buttons.
- Funktioniert auch ohne SyncPlay und in Ansichten, die nur das Benutzermenü zeigen.
- Kopfzeilenersetzung, Anmeldung/Abmeldung und Sprachwechsel erzeugen keine doppelten Buttons.
- Darstellung und Fokusmarkierung für die neue Kopfzeile ergänzt.

Die Ursache wurde an einer laufenden Jellyfin-Installation lesend bestätigt. Die Korrektur wurde lokal mit der entsprechenden Kopfzeilenstruktur getestet. Während der Diagnose wurden keine Plugin-Einstellungen geändert und kein Serverneustart ausgelöst.

Geprüft: 47 .NET-Tests, elf JavaScript-Tests, Release-Build und Browserprüfung mit neuer und klassischer Kopfzeile. Die lokale Prüfansicht kann mit `node tests/web/preview-navbar.cjs` gestartet werden; sie verbindet sich mit keinem Jellyfin-Server.

Das Update steht im bestehenden Katalog bereit. Installation und den notwendigen Jellyfin-Neustart bitte selbst zu einem passenden Zeitpunkt durchführen. Danach die Weboberfläche mit **Strg + F5** neu laden.
