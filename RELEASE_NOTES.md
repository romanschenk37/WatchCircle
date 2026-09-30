# WatchCircle 1.0.8.0 — Profile, Navigation und Sprache

- Profile zeigen den Fortschritt wie die Film-/Seriendetailseite: Staffel, Folge, Wiedergabedauer und Fortschritt der jeweiligen Folge – für die andere Person und dich. Es gelten dieselben Regeln für die am weitesten begonnene Folge.
- Sammlungsüberschriften wie „Begonnene Serien ›“ öffnen direkt die Sammlung. Der zusätzliche Button „Alle anzeigen“ rechts entfällt. Die Überschriften sind auch über Pfeiltasten erreichbar.
- Das Personen-Symbol „WatchCircle“ neben dem bisherigen Gemeinsam-schauen-Button wird nach der Anmeldung und nach dem Neuladen der Jellyfin-Kopfzeile zuverlässig ergänzt. Das Symbol benötigt keine externe Symbolschrift.
- Die Plugin-Einstellungen erscheinen links im Dashboard unter „Plugins → WatchCircle“.
- Alle Plugin-Texte richten sich nach der Jellyfin-Anzeigesprache: Deutsch oder Englisch, bei anderen Sprachen einheitlich Englisch wie im ursprünglichen Plugin. Das umfasst Profile, Fortschrittskarten, Gruppen-/Seerr-Einstellungen und die übernommenen Gemeinsam-schauen-Dialoge.

Eine Serie gilt weiterhin erst dann als abgeschlossen, wenn alle verfügbaren, für dich sichtbaren Folgen einschließlich Specials gesehen wurden. „Folge abgeschlossen“ im Fortschrittsbalken bezieht sich auf die angezeigte Folge. Profile verwenden ausschließlich bestehende Jellyfin-Daten; Gruppenrechte und Favoritenzuordnung bleiben erhalten.

Geprüft: 47 .NET-Tests, elf JavaScript-Tests, Release-Build und Browserprüfung mit Beispieldaten (Staffel-/Folgenvergleich, Sammlungslinks, Anmeldung, ersetzte Kopfzeile, Deutsch/Englisch, englische Ersatzsprache, Einstellungen und simulierte Fernbedienungstasten). Kein Zugriff auf den produktiven Server und keine Tests auf echten TV-Geräten.

Update über den bestehenden Plugin-Katalog installieren, Jellyfin zu einem passenden Zeitpunkt neu starten und die Weboberfläche mit **Strg + F5** neu laden.

Katalog: `https://raw.githubusercontent.com/romanschenk37/WatchCircle/master/manifest.json`
