# WatchCircle 1.1.4.0 — Sortierung und gemischte Sammlungen

- Unter **Bibliothek aufräumen → Verwaltung → Sortierung** lassen sich die Inhalte alphabetisch (A–Z / Z–A), nach Speicherplatzbedarf (grösste / kleinste zuerst) und nach letzter Interaktion (älteste / neuste zuerst) ordnen. Die Gruppierung in Serien, Filme und Sammlungen bleibt erhalten.
- Die Auswahl bleibt beim Wechsel der Statusfilter und bei der Rückkehr aus einer Detailansicht erhalten. Das Umsortieren erfolgt ohne erneutes Laden der Kacheln oder Seerr-Anfragen. Ohne erfasste Interaktion wird der angezeigte Beginn der Startfrist verwendet.
- Interaktionen mit Filmen **und Serien** berücksichtigen alle Filme und Serien in den Sammlungen, denen der Titel direkt angehört. Offene Löschvormerkungen entfallen dort und die Inaktivitätsfrist beginnt neu. Eine einzelne Folge gilt weiterhin für die ganze Serie. Dauerhafter Sammlungsschutz berücksichtigt ebenfalls Serien.
- Die Verwaltung zeigt die weitergegebene letzte Interaktion mit Zeitpunkt, Benutzer und Art bei den betroffenen Titeln an und verwendet sie für die Sortierung. Neuere Interaktionen werden durch ältere nachgelesene Wiedergaben nicht überschrieben.
- Sammlungsdetails und Rückmeldungstexte berücksichtigen Filme und Serien. Die Weitergabe erfolgt nicht transitiv über weitere Sammlungen anderer Mitglieder.

Nach dem Update **Bibliothek aufräumen: Inhalte prüfen** einmal ausführen, damit die Mitgliedschaften gemischter Sammlungen eingelesen werden. Die Erweiterung gilt für danach erfasste Interaktionen; frühere Serieninteraktionen werden nicht rückwirkend rekonstruiert.

Geprüft: 114 .NET-Tests und 30 JavaScript-Tests erfolgreich, darunter Sortierrichtungen, Sprachvarianten, Filterwechsel, gemischte Sammlungen, Interaktionszeitpunkte und Löschvormerkungen. Lokale Browserprüfung einschliesslich Tastaturbedienung und schmaler Ansicht. Der externe Arr-Livetest wurde nicht ausgeführt; die Löschtests verwenden isolierte Testdateien.

Nach der Installation Jellyfin zu einem passenden Zeitpunkt selbst neu starten und die Weboberfläche vollständig neu laden.
