# WatchCircle 1.0.6.0 — Personen und Fortschrittsvergleich

Der neue **WatchCircle**-Button in der Jellyfin-Weboberfläche zeigt alle Personen, mit denen du mindestens eine Gruppe teilst. Jede Person erscheint nur einmal.

- Öffne ein Profil aus der Personenliste oder über einen Namen bzw. ein Profilbild in der WatchCircle-Kachel.
- **Begonnen** zeigt angefangene Filme und Serien mit dem Fortschritt der Person und deinem eigenen Fortschritt.
- **Abgeschlossen** zeigt gesehene Filme und Serien auf dem aktuellen Stand, ebenfalls mit deinem Fortschritt zum Vergleich.
- **Favoriten** zeigt favorisierte Filme und Serien, die die Person noch nicht begonnen hat. Bereits begonnene oder abgeschlossene Favoriten erscheinen in ihrer jeweiligen Kategorie.
- Suche, Film-/Serienfilter und schrittweises Erweitern größerer Listen sind enthalten.
- Seerr-Antragsteller sind anklickbar, wenn Seerr ihre Jellyfin-ID liefert und ihr eine gemeinsame Gruppe habt. Andere Antragsteller bleiben als Text sichtbar.

Serienfortschritt wird über alle für dich verfügbaren Folgen inklusive Specials berechnet. Fehlende/virtuelle Folgen zählen nicht; beide Personen verwenden dieselbe Gesamtzahl. Eine abgeschlossene Serie bedeutet, dass alle aktuell verfügbaren Folgen gesehen wurden. Neue Folgen können sie wieder unter „Begonnen“ einordnen. Die bestehende Detailkachel zeigt weiterhin die am weitesten begonnene Folge.

Profile lesen ausschließlich vorhandene Jellyfin-Fortschritte und Favoriten. Es wird keine zusätzliche Profil- oder Wiedergabehistorie gespeichert. Der Server prüft gemeinsame Gruppen und deine Bibliotheksberechtigungen. Die geerbte Funktion „Watch together“ bleibt weiterhin vorhanden.

Prüfung: 42 automatisierte Tests erfolgreich, Release-Build ohne Fehler oder Warnungen, JavaScript-Syntaxprüfung und Browserprüfung mit simulierten Jellyfin-/Seerr-Daten. Der Test auf einem echten Jellyfin-Server steht noch aus.

Update über den bestehenden Plugin-Katalog installieren, Jellyfin zu einem passenden Zeitpunkt neu starten und die Weboberfläche mit **Strg + F5** neu laden. Profile stehen in der Jellyfin-Weboberfläche zur Verfügung.

Katalog: `https://raw.githubusercontent.com/romanschenk37/WatchCircle/master/manifest.json`
