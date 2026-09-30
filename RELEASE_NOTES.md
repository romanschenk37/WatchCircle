# WatchCircle 1.1.0.0 — Bibliothek aufräumen

Neuer optionaler Bereich mit eigenen Einstellungen und Verwaltung. **Standardmässig ausgeschaltet; Löschung zunächst manuell.**

- Konfigurierbare Mediatheken, Inaktivitätsfrist, Vorwarnfrist und Auswertungsintervall; konservative Startfrist für unbekannte Vorgeschichte.
- Rückmeldungen beim Öffnen/Wiederaufnehmen der Weboberfläche und über einen dauerhaften Papierkorb-Menüpunkt.
- **Vorgemerkte Poster sind markiert. Detailseiten zeigen einen Banner mit Löschdatum und eigener Antwort. Dort lässt sich auch ein früheres „Mir doch egal“ in „Bitte noch nicht löschen“ ändern.**
- Aktivität aller Benutzer schützt ganze Serien beziehungsweise betroffene Filmsammlungen. Historische Antworten bleiben nachvollziehbar.
- Dauerhafter Schutz für Filme, Serien und Sammlungen; eigene Admin-Kachel mit allen Benutzern, Gesamtfortschritt und Seerr-Anfragenden.
- Radarr-/Sonarr-Verbindungen, Container-Pfadzuordnung und separate Importlisten-Ausschlüsse. Dateien werden ausschliesslich über die Dienste entfernt, ihre Papierkörbe bleiben wirksam.
- Erneute Prüfungen direkt vor der Löschung, dauerhafte Protokolle und Überprüfung unterbrochener Vorgänge.
- Kleines ergänzendes Gesehen-Archiv pro Benutzer und Film/Folge für eindeutig wiedererkannte Inhalte; neuere Ungesehen-Entscheidungen haben Vorrang.

Die normalen WatchCircle-Gruppen und Fortschrittsanzeigen behalten ihre bisherigen Regeln. Die Aufräumfunktion speichert ihren Ablauf separat.

Geprüft: automatisierte .NET- und JavaScript-Tests, lokale Browserprüfung sowie echte API-Löschungen erzeugter Testmedien mit **Radarr 6.3.0.10514** und **Sonarr 4.0.20.3014**, jeweils mit Papierkorbprüfung. Die vollständige Laufzeitprüfung auf Jellyfin 12.1 und Tests auf echter TV-Hardware stehen noch aus.

[Einrichtung, Datensicherung, Aufbewahrung und genaue Testgrenzen](https://github.com/romanschenk37/WatchCircle/blob/master/docs/library-cleanup.md).

Installation und den dafür erforderlichen Jellyfin-Neustart bitte selbst zu einem passenden Zeitpunkt durchführen. Anschliessend die Weboberfläche vollständig neu laden. Dein produktiver Server wurde nicht verändert oder neugestartet.
