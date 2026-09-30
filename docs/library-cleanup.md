# Bibliothek aufräumen

WatchCircle 1.1.0.0 ergänzt einen separaten, optionalen Bereich. Er ist nach der Installation ausgeschaltet. Auch nach dem Einschalten bleibt die Löschung zunächst manuell. Bestehende Gruppen, Profile und normale Fortschrittsanzeigen verwenden weiterhin Jellyfin-Daten mit ihren bisherigen Sichtbarkeitsregeln.

## Einrichtung

**Dashboard → WatchCircle · Cleanup** oder der Link **Bibliothek aufräumen** in den WatchCircle-Einstellungen öffnen die Verwaltung.

1. Unter **Einstellungen** aktivieren und Mediatheken auswählen.
2. Inaktivitätsfrist, Vorwarnfrist und Auswertungsintervall festlegen. Vorgaben: **3 Kalendermonate, 30 Tage, 24 Stunden**.
3. Vom Jellyfin-Server erreichbare Radarr-/Sonarr-URLs und API-Schlüssel speichern. Bei einer URL-Änderung ist der Schlüssel erneut nötig. **Gespeicherte Verbindung testen** prüft die gespeicherte Verbindung.
4. Unterschiedliche Containerpfade als Stammordner zuordnen, etwa Jellyfin `/media/movies` → Radarr `/movies`. Ohne Zuordnung werden gleiche Pfade vorausgesetzt. Mehrdeutige Zuordnungen werden abgelehnt.
5. **Jetzt auswerten** liest den Bestand und prüft unterbrochene Vorgänge. Eine leere Kandidatenliste unmittelbar nach Aktivierung ist wegen der konservativen Startfrist normal.

Papierkörbe werden in Radarr/Sonarr eingestellt. Importlisten-Ausschlüsse sind pro Dienst separat einstellbar und zunächst aus. Automatische Löschung muss zusätzlich ausdrücklich aktiviert werden.

## Vormerkung und Rückmeldung

Nach der Inaktivitätsfrist beginnt eine Vormerkung mit voller Vorwarnfrist. Wiederholte Auswertungen verschieben ihr Datum nicht. Geänderte Vorwarnfristen gelten für neue Vormerkungen.

Ein Papierkorb-Symbol oben führt jederzeit zu offenen Rückmeldungen. Beim Öffnen oder Wiederaufnehmen der unterstützten Weboberfläche wird dieselbe Liste automatisch angeboten. Während eines anderen Dialogs oder des erkannten Web-Videoplayers wartet sie bis zur nächsten Prüfung.

- **Mir doch egal:** beendet weitere Nachfragen an diesen Benutzer, bis er wieder mit dem betroffenen Umfang interagiert. Es zählt nicht als Aktivität und verkürzt keine Frist.
- **Bitte noch nicht löschen:** hebt die Vormerkung für alle auf und beginnt die Inaktivitätsfrist neu. Kein dauerhafter Schutz.
- **Keine Antwort:** gilt nicht als Zustimmung, verhindert allein aber nicht das Erreichen des Löschdatums.

Vorgemerkte Poster tragen eine sichtbare Warnmarkierung. Ein Detailbanner zeigt das frühestmögliche Löschdatum, die eigene Antwort und die Antwortknöpfe. Nach geschlossenem Popup oder früherem **Mir doch egal** lässt sich dort weiterhin **Bitte noch nicht löschen** wählen. Staffel- und Folgenseiten beziehen sich auf die ganze Serie. Im manuellen Modus ist das Datum die früheste Berechtigung, kein garantierter Löschzeitpunkt.

Eine veraltete Ansicht kann keine aufgehobene Vormerkung wieder aktivieren. Bereits an Arr gesendete Aufträge können nicht per Rückmeldung zurückgerufen werden; der Banner zeigt dann die ausstehende Bestätigung. Statusänderungen erscheinen bei Navigation, Rückmeldungen und der nächsten Webprüfung, normalerweise innerhalb von 30 Sekunden. Normale Benutzer sehen nur zugängliche Titel und ihre eigene Antwort.

## Aktivität und Startfristen

Serverseitig zählen Wiedergabestart/-fortschritt/-ende, manuelle Änderungen des Gesehen-Status, neu hinzugefügte Favoriten und **Bitte noch nicht löschen**. Vorhandene Favoriten verlängern die Frist nicht bei jedem Durchlauf. Detailseitenaufrufe und reine Metadatenänderungen zählen nicht als Wiedergabe.

Eine Folge schützt die ganze Serie. Ein Film schützt alle Filme jeder Jellyfin-Sammlung, der er direkt angehört. Diese Ausbreitung ist nicht beliebig transitiv über weitere Sammlungen anderer Filme.

Alle Jellyfin-Benutzer zählen, einschliesslich Administratoren und Personen ausserhalb gemeinsamer Gruppen. Nur die aktive **Mir doch egal**-Antwort des interagierenden Benutzers wird im betroffenen Umfang zurückgesetzt. Historie und Antworten anderer Benutzer bleiben erhalten.

Jellyfin hat keine zuverlässigen historischen Zeitstempel für jede Favoriten- und Gesehen-Änderung. Bestehende Titel erhalten bei Aktivierung deshalb eine volle Startfrist. Neue, erneut hinzugefügte oder an neue Dateipfade verschobene Inhalte ebenfalls. Eine zusätzliche Folge setzt die Startfrist ihrer Serie zurück. Vorhandene jüngere Wiedergabezeitstempel werden berücksichtigt.

## Verwaltung und dauerhafter Schutz

Die Verwaltung trennt fällige und bevorstehende Löschungen. Weitere Filter zeigen auf Wunsch behaltene, dauerhaft geschützte, gelöschte, fehlgeschlagene/unvollständige und alle Titel. Einträge führen zu letzter Aktivität, Vormerkung, Speicherbedarf, Arr-Zuordnung, Antworten, offenen Rückmeldungen und Löschprotokoll.

Die Admin-Kachel zeigt **alle** bestehenden Benutzer, auch den angemeldeten Administrator und Personen ohne begonnenen Fortschritt. Serien verwenden dieselbe Gesamtfortschrittsberechnung wie normale WatchCircle-Kacheln. Seerr-Anfragende und frühere Antworten stehen daneben. Sammlungen verlinken einzelne Filme. Für entfernte Inhalte ohne aktuelle Jellyfin-Daten wird kein Fortschritt erfunden. Die Admin-Endpunkte verlangen serverseitig Jellyfins Administratorberechtigung.

Administratoren können Filme, Serien und Sammlungen auf Detailseiten oder in der Verwaltung **dauerhaft behalten**. Schutz gilt auch für neue Folgen/Sammlungsmitglieder, übersteht Neustarts und wird bei eindeutiger Identität auf neue Jellyfin-IDs übertragen. Noch nicht versendete Aufträge werden ungültig. Nach Aufheben beginnt eine neue Startfrist; eine spätere Vormerkung bekommt die volle Warnfrist.

## Löschablauf

Eine manuelle Bestätigung zeigt den konkreten Umfang und gilt 15 Minuten. Sammlungen werden pro Film verarbeitet/protokolliert. Geschützte oder nicht fällige Filme können nicht in einen solchen Auftrag aufgenommen werden.

Unmittelbar vor jedem Versand werden Bestand, Mediathekenauswahl, Schutz, Fristen, neue Aktivität und Wiedergabesitzungen erneut geprüft. Auch pausierte Sitzungen im betroffenen Umfang verhindern die Löschung. Fehler der Zustandsdatei blockieren sie.

Die Zuordnung verlangt eindeutige **TMDB-Film-IDs** beziehungsweise **TVDB-Serien-IDs** und exakte Übereinstimmung der Arr-Mediendateien mit Jellyfin nach Pfadübersetzung. Titelgleichheit genügt nicht. Fehlende IDs/Dateien, abweichende Bestände oder zusätzliche noch nicht in Jellyfin gescannte Arr-Folgen blockieren den Auftrag.

- Radarr entfernt den Filmeintrag mit `deleteFiles=true`.
- Sonarr entfernt die **ganze Serie inklusive Dateien** und überwacht danach auch zukünftige Folgen nicht mehr.
- Arr-Papierkörbe bleiben wirksam. WatchCircle hat keinen Ersatzweg über direkte Dateilöschung.
- Seerr-Anfragen und ihre Historie werden nicht gelöscht.

HTTP-Erfolg allein genügt nicht: Arr-Eintrag verschwunden, ursprüngliche Dateien bei erreichbarem Bibliotheksstamm entfernt, Jellyfin-Scan abgeschlossen und ursprünglicher Eintrag aus dem Bestand entfernt. Sonst bleibt der Vorgang unvollständig. Bei Neustart/erneuter Auswertung werden offene Vorgänge zunächst **lesend verifiziert**. Ein neuer manueller Löschversuch braucht eine neue Bestätigung und dieselben Prüfungen. Fehlt der Arr-Eintrag bereits, während Dateien liegen bleiben, muss die Ursache im betreffenden Dienst behoben werden; WatchCircle löscht diese Dateien nicht selbst.

## Gesehen-Status und Archiv

Die untersuchte Jellyfin-Implementierung löst Nutzerdaten bei Entfernung vom Bibliothekseintrag und kann sie anhand ihrer `CustomDataKey`/`GetUserDataKeys` wieder zuordnen. Neue Pfade/Item-IDs schliessen dies nicht grundsätzlich aus; wiedererkannte Schlüssel sind entscheidend. Die Nutzerdaten-Aufräumaufgabe entfernt abgetrennte Datensätze nach 90 Tagen, wenn sie ausgeführt wird; im untersuchten Stand hat sie keinen voreingestellten Zeitplan. Das ist keine unbegrenzte Garantie bei beliebigen Metadaten-/Episodenänderungen.

Quellen: [BaseItemRepository, Jellyfin 10.11.11](https://github.com/jellyfin/jellyfin/blob/v10.11.11/Jellyfin.Server.Implementations/Item/BaseItemRepository.cs), [CleanupUserDataTask](https://github.com/jellyfin/jellyfin/blob/v10.11.11/Emby.Server.Implementations/ScheduledTasks/Tasks/CleanupUserDataTask.cs), [UserDataManager](https://github.com/jellyfin/jellyfin/blob/v10.11.11/Emby.Server.Implementations/Library/UserDataManager.cs).

WatchCircle sichert daher vor eigenen Löschungen zusätzlich **nur den Gesehen-Status pro Benutzer und Film/Episode**, mit stabilen Anbieterkennungen. Für einzelne Folgen ist als Ersatz eine eindeutige Serienkennung mit Staffel/Folge möglich. Mehrfachfolgen ohne eigene stabile Kennung werden nicht darüber erraten. Pfad/Item-ID allein genügt nicht.

Vorhandene Jellyfin-Daten haben Vorrang. Archiviertes `gesehen=true` wird nur bei eindeutiger Zuordnung ohne neuere manuelle Entscheidung ergänzt. Ungesehene/neue Folgen werden nicht als gesehen markiert. Wiederholte Wiederherstellung desselben Eintrags wird verhindert. Über `IUserDataManager` mit Import-Grund wird ausschliesslich `Played` gesetzt; Zähler, Position, Favoriten und Zeitstempel werden nicht künstlich fortgeschrieben. Archive abgebrochener oder noch nicht vollständig bestätigter Löschungen werden nicht angewendet. Wird ein Inhalt schon vor der Bestätigung am selben Pfad erneut hinzugefügt, ist eine automatische Bestätigung unter Umständen nicht mehr eindeutig möglich; der Vorgang bleibt zur Prüfung offen.

## Sicherung

Zusätzlich zur bisherigen Konfiguration:

```text
<Jellyfin PluginConfigurationsPath>/WatchCircle/cleanup-state.json
<Jellyfin PluginConfigurationsPath>/WatchCircle/cleanup-state.json.bak
```

Je nach Installation etwa unter `/var/lib/jellyfin/plugins/configurations/` oder im Konfigurationsvolume. Die Dateien liegen ausserhalb des versionsabhängigen DLL-Verzeichnisses und bleiben bei Updates erhalten. Auch die Jellyfin-Datenbank weiterhin sichern.

Gespeichert werden Einstellungen einschliesslich Arr-Schlüsseln, Inhaltsidentitäten, notwendige Interaktionsstände, Vormerkungen, Antwortgeschichte, dauerhafter Schutz, Bestätigungspläne, Protokolle und Gesehen-Archiv. Die Datei wird atomar ersetzt und vor Löschaufträgen auf den Datenträger geschrieben. `.bak` enthält den vorherigen Stand. Beide gehören in eine geschützte Serversicherung. Die API gibt gespeicherte Schlüssel nicht zurück. Defekte Dateien werden nicht still geleert; nach Reparatur einer blockierten Speicherung ist ein geplanter Jellyfin-Neustart nötig.

## Prüfung und Grenzen

| Bestandteil | Tatsächliche Prüfung |
| --- | --- |
| Jellyfin | Kompiliert gegen die bereits verwendeten Pakete **10.11.11**, .NET 9; Katalog-ABI bleibt **12.0.0.0** für die gemeldete Installation **12.1**. Vollständige Laufzeitprüfung der neuen Funktion dort noch offen. |
| Radarr **6.3.0.10514** | Offizielles Windows-Paket, getrennte lokale Instanz: echte Zuordnung/Löschung erzeugter AVI-Testdatei, Eintrag/Quelldatei entfernt und Papierkorbdatei nachgewiesen. |
| Sonarr **4.0.20.3014** | Entsprechender isolierter Test einer erzeugten Folge mit Entfernung des Serieneintrags und Papierkorbprüfung. |
| Browser | Produktives JavaScript mit lokalen Beispieldaten: Deutsch/Englisch, 390-px-Ansicht, 1920×1080, Pfeiltasten/OK/Schliessen, Antwortänderung und Entfernen der Posterwarnung. |
| Samsung / LG / Android TV | Keine echte Hardware geprüft. UI nur bei Clients, die die erweiterte Server-Weboberfläche laden. LG/webOS praktisch noch offen. Native Apps bekommen keine eigene WatchCircle-Oberfläche; ihre Serverereignisse zählen trotzdem. |

Tests decken Fristen, Sammlungsschutz, Benutzerantworten, Berechtigungen, letzte Änderungen vor Versand, aktive Wiedergabe, Speicherfehler, uneindeutige Identitäten, unterbrochene Vorgänge, neue IDs/Pfade, verschiedene Gesehen-Zustände, neue Folgen, neuere Ungesehen-Entscheidungen und bestehende Funktionen ab. Die Arr-Tests nutzen echte Dienste, ersetzen aber Jellyfins Bibliotheksadapter. Der vorgesehene Jellyfin-Aufruf zur Wiederherstellung ist ebenfalls geprüft. Echter Jellyfin-Scan/Wiedergabe über den vollständigen Ablauf stehen noch aus.

Prüfungen: `dotnet test Jellyfin.Plugin.WatchCircle.sln -c Release` und `node --test tests/web/*.test.cjs`. Die opt-in `CleanupLiveArrTests.cs` erwarten isolierte Dienste auf localhost:17878/18989, erzeugte Testmedien und `WATCHCIRCLE_ARR_TEST_ROOT=<Repo>/artifacts/cleanup-live`. Datenordner, Versionen und Testpfade werden vor einem Auftrag überprüft. Ohne Variable werden sie übersprungen. `node tests/web/preview-cleanup.cjs` startet die lokale Browservorschau auf localhost:8769; deren Beispieldaten-Bedienelemente sind nicht im Plugin eingebettet.

Deaktivierung beendet künftige Auswertungen, Erfassung und Löschaufträge. Bereits versendete Arr-Aufträge kann sie nicht zurückrufen. Kein produktiver Server wurde für die Entwicklung verändert oder neugestartet.

### Isolierte Arr-Prüfung wiederholen

Die offiziellen portablen Windows-Pakete aus den genannten GitHub-Releases getrennt nach `artifacts/cleanup-live/radarr` und `sonarr` entpacken. Eigene Datenordner `radarr-data` und `sonarr-data` in diesem Testverzeichnis verwenden, niemals bestehende Installationen. Deren `config.xml` muss `BindAddress=127.0.0.1`, Port `17878` beziehungsweise `18989`, `LaunchBrowser=False`, einen eigenen Test-API-Schlüssel und abgeschaltete automatische Updates enthalten. Die Konsolenprogramme mit `-nobrowser` und `-data="<absoluter Testdatenordner>"` starten.

`node tests/integration/prepare-arr.cjs` erzeugt stille AVI-Testbilder, holt öffentliche Metadaten, importiert die Dateien und richtet ausschliesslich in diesen Instanzen Test-Papierkörbe ein. Es prüft die gemeldeten Datenordner und schreibt `fixtures.json`. Danach `WATCHCIRCLE_ARR_TEST_ROOT` auf den absoluten Teststamm setzen und `dotnet test -c Release --filter FullyQualifiedName~CleanupLiveArrTests` ausführen. Diese beiden Tests entfernen nur die erzeugten Testeinträge über den produktiven Plugin-Code. Abschliessend die selbst gestarteten Testprozesse beenden. Die Schlüssel und Datenordner werden durch `.gitignore` nicht veröffentlicht.
