# WatchCircle 1.0.7.0 — Posterreihen und Fernbedienungs-Navigation

Die Profile orientieren sich jetzt an der Jellyfin-Startseite: große Poster in Sammlungsreihen untereinander. Das Suchfeld und die Kategorie-/Medienfilter im Profil wurden entfernt.

- Sechs Reihen: begonnene Filme, begonnene Serien, abgeschlossene Filme, abgeschlossene Serien, favorisierte Filme und favorisierte Serien.
- Unter jedem Poster bleiben der Fortschritt der Person und dein Fortschritt sichtbar.
- Links/rechts bewegt den Fokus innerhalb einer Reihe, hoch/runter zwischen den Reihen. OK/Enter öffnet den Titel oder die Sammlung.
- Zurück/Escape führt von einer Sammlung zum Profil, dann zur Personenliste und zurück zu Jellyfin. Fokus und Scrollposition bleiben beim Zurückkehren erhalten.
- „Alle anzeigen“ öffnet große Sammlungen als Raster. Weitere Titel lassen sich schrittweise laden; keine Titel werden dauerhaft abgeschnitten.
- Deutlich sichtbarer Fokus, automatisches Scrollen zum ausgewählten Titel, Tastatur-Fokus innerhalb der Ansicht und Behandlung der Samsung-/LG-Zurück-Tastencodes.
- Die optionale Suche in der Personenliste bleibt erhalten. Im geöffneten Profil gibt es kein Suchfeld.

WatchCircle erweitert weiterhin die vom Server ausgelieferte Weboberfläche. Die offizielle LG-webOS-App lädt diese Oberfläche und kommt grundsätzlich dafür infrage; echte Gerätetests stehen noch aus. Die offizielle Samsung-Tizen-App bringt ihre eigene Weboberfläche mit, Android TV verwendet eine native Oberfläche. Dort ist eine zusätzliche Client-Integration nötig; diese Version ergänzt keine native TV-App.

Fortschrittsberechnung, Gruppenrechte und die Speicherung bleiben unverändert. Die Profile lesen vorhandene Jellyfin-Daten ohne zusätzliche Historie. Entwickler-Testbuttons und künstliche Testtitel sind ausschließlich Teil der lokalen Vorschau und werden nicht veröffentlicht.

Geprüft: 42 .NET-Tests, sieben JavaScript-Navigationstests, erfolgreicher Release-Build und Browserprüfung mit Beispieldaten (Pfeiltasten, OK/Zurück, simulierte Samsung-/LG-Tasten, 122 Titel, Fokuswiederherstellung, schmale Darstellung und Full HD). Keine Tests auf echten Fernsehern oder dem produktiven Jellyfin-Server.

Update über den bestehenden Plugin-Katalog installieren, Jellyfin zu einem passenden Zeitpunkt neu starten und die Weboberfläche mit **Strg + F5** neu laden.

Katalog: `https://raw.githubusercontent.com/romanschenk37/WatchCircle/master/manifest.json`
