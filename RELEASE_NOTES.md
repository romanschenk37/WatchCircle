# WatchCircle 1.0.10.0 — Gesamtfortschritt für Serien und Staffeln

Serien und Staffeln zeigen jetzt den Fortschritt des gesamten ausgewählten Titels an, statt nur den Fortschritt einer einzelnen Folge. Das gilt auch für Serien in den Profilen und den Vergleich mit dem eigenen Fortschritt.

- **Serie:** beispielsweise „6 von 12 Folgen abgeschlossen (54 %)“, mit Gesamtbalken über alle verfügbaren Folgen inklusive Specials.
- **Staffel:** dieselbe Anzeige, begrenzt auf die Folgen dieser Staffel.
- **Einzelne Folge und Film:** weiterhin Laufzeitfortschritt in Minuten und Prozent.
- Die zusätzliche Angabe von Staffel und Folge trägt jetzt **Begonnen** oder **Gesehen**. Sie zeigt die am weitesten begonnene Folge, nicht die nächste Folge.
- Angefangene Folgen zählen anteilig zum Gesamtfortschritt. Übersprungene Folgen werden nicht als gesehen angenommen. 100 % wird erst angezeigt, wenn alle verfügbaren Folgen von Jellyfin als gesehen markiert sind.
- Detailseiten und Profile verwenden dieselbe Berechnung und berücksichtigen dieselben für den Betrachter verfügbaren Folgen. Fehlende und virtuelle Folgen zählen nicht mit.
- Deutsch und Englisch folgen weiterhin der Jellyfin-Anzeigesprache.

Es werden ausschließlich vorhandene Jellyfin-Fortschrittsdaten gelesen; für diese Anzeige wird keine zusätzliche Historie gespeichert.

Geprüft: 51 .NET-Tests, 16 JavaScript-Tests, Release-Build und lokale Browserprüfung der Serien-, Staffel-, Folgen- und Profilansicht. Die neue Version wurde noch nicht auf dem produktiven Jellyfin-Server getestet.

Das Update steht im bestehenden Katalog bereit. Installation und den notwendigen Jellyfin-Neustart bitte selbst zu einem passenden Zeitpunkt durchführen. Danach die Weboberfläche mit **Strg + F5** neu laden.
