# WatchCircle 1.1.2.0 — Aufräumübersicht nach Speicherbedarf

- Die Verwaltung unter **Bibliothek aufräumen** gruppiert Inhalte nach **Serien** und **Filmen**, jeweils mit dem grössten geschätzten Speicherbedarf zuerst. Vorhandene Sammlungen haben einen eigenen Abschnitt. Alle Statusfilter bleiben verfügbar; fällige Löschtermine sind auf den Kacheln markiert.
- **Angefragt von** zeigt Seerr-Antragsteller direkt auf den Kacheln vorhandener Titel. Die Namen werden beim Scrollen nachgeladen, ohne die Übersicht zu blockieren.
- In der Detailansicht stehen Antragsteller zuerst in der Benutzerliste. Die doppelte Angabe am Seitenende entfällt. Seerr-Konten ohne Jellyfin-Verknüpfung werden ausdrücklich gekennzeichnet und keinem gleichnamigen Konto zugeordnet.

Die regelmässige Auswertung besteht bereits: Bei aktivierter Aufräumfunktion läuft sie nach dem Serverstart und danach im konfigurierten Intervall (standardmässig **24 Stunden**). Sie läuft als Hintergrunddienst, ohne separaten Eintrag unter Jellyfins geplanten Aufgaben. Die Einstellungen und Löschregeln bleiben unverändert.

Geprüft: automatisierte JavaScript- und .NET-Tests sowie lokale Browserprüfung mit Beispieldaten. Eine Prüfung dieser Version auf dem produktiven Jellyfin-Server und auf echter TV-Hardware steht noch aus.

Installation und den dafür erforderlichen Jellyfin-Neustart bitte selbst zu einem passenden Zeitpunkt durchführen. Anschliessend die Weboberfläche vollständig neu laden.
