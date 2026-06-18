# kristijanMikroservisi
Tehnološki stek
C# / .NET (ASP.NET Core MVC) – razvoj kompletne aplikacije
Entity Framework Core – rad sa bazom podataka
SQL Server – baza podataka
Razor (cshtml) – frontend unutar ASP.NET MVC
HTML / CSS / JavaScript – korisnički interfejs
Visual Studio 2026 – razvojno okruženje

Arhitektura
Aplikacija je razvijena kao monolitna web aplikacija koristeći MVC (Model-View-Controller) obrazac u okviru ASP.NET Core.

Model – predstavlja podatke sistema (Dogadjaj, Lokacija, Predavac, Prijava) i komunikaciju sa bazom
View (Razor) – prikaz podataka kroz .cshtml stranice
Controller – obrada zahteva i povezivanje Modela i View-a
<img width="1324" height="922" alt="image" src="https://github.com/user-attachments/assets/add91d02-6472-4faa-8467-df27f97ecf5e" />
## Implementirani mikroservisni obrasci i napredni koncepti

Projekat je tokom razvoja nadograđen implementacijom više mikroservisnih obrazaca i tehnika koje se koriste u modernim distribuiranim sistemima.

### Mikroservisna arhitektura

Sistem je podeljen na više nezavisnih servisa:

* MVC aplikacija (glavna aplikacija)
* Predavač API
* Događaj API
* Lokacija API
* Saga Orchestrator
* API Gateway
* Shared biblioteka za zajedničke modele i događaje

Svaki servis poseduje sopstvenu odgovornost i može se razvijati, testirati i izvršavati nezavisno od ostalih servisa.

---

## API Gateway

Za centralizovan pristup mikroservisima implementiran je API Gateway korišćenjem Ocelot biblioteke.

Gateway omogućava:

* Jedinstvenu ulaznu tačku za klijente
* Rutiranje zahteva prema odgovarajućim servisima
* Transformaciju zahteva
* Logovanje svih dolaznih zahteva
* Implementaciju bezbednosnih mehanizama
* Jednostavnije upravljanje komunikacijom između klijenta i servisa

Implementirani middleware-i:

* Request Logging Middleware
* Request Transformation Middleware
* API Key Middleware (bezbednosni filter)

---

## RabbitMQ asinhrona komunikacija

Za međuservisnu komunikaciju korišćen je RabbitMQ Message Broker.

Prednosti ovakvog pristupa:

* Slabo povezani servisi (Loose Coupling)
* Veća skalabilnost
* Veća otpornost sistema
* Asinhrona obrada događaja
* Pouzdana razmena poruka

RabbitMQ se koristi za:

* Razmenu poslovnih događaja između servisa
* Saga komunikaciju
* Validaciju podataka između servisa
* Obradu email poruka
* Sinhronizaciju podataka između mikroservisa

---

## Outbox Pattern

Implementiran je Outbox obrazac radi garantovane isporuke događaja.

Prilikom izvršavanja poslovne operacije:

1. Podaci se upisuju u bazu.
2. Događaj se upisuje u Outbox tabelu u istoj transakciji.
3. Background servis periodično čita Outbox tabelu.
4. Događaji se objavljuju na RabbitMQ.
5. Nakon uspešnog slanja događaj se označava kao obrađen.

Prednosti:

* Sprečava gubitak poruka
* Obezbeđuje konzistentnost sistema
* Omogućava pouzdanu integraciju između servisa

Implementacije:

* OutboxMessage
* OutboxMessagePublisher
* OutboxDispatcher

---

## CQRS (Command Query Responsibility Segregation)

U okviru Lokacija servisa implementiran je CQRS obrazac.

CQRS razdvaja:

### Commands

Operacije koje menjaju stanje sistema:

* Kreiranje lokacije
* Izmena lokacije
* Brisanje lokacije

Implementirani Command Handler-i:

* KreirajLokacijuCommandHandler
* IzmeniLokacijuCommandHandler
* ObrisiLokacijuCommandHandler

### Queries

Operacije za čitanje podataka:

* Prikaz svih lokacija
* Prikaz detalja lokacije
* Filtriranje po kapacitetu

Implementirani Query Handler-i:

* GetSveLokacijeQueryHandler
* GetLokacijaDetaljiQueryHandler
* GetLokacijePoKapacitetuQueryHandler

Prednosti:

* Jasno razdvajanje odgovornosti
* Jednostavnije održavanje sistema
* Bolja skalabilnost
* Lakše testiranje

---

## Event Sourcing

Lokacija servis koristi Event Sourcing za čuvanje istorije promena.

Umesto čuvanja samo trenutnog stanja, sistem čuva sve događaje koji su doveli do trenutnog stanja.

Primer događaja:

* LokacijaCreated
* LokacijaUpdated
* LokacijaDeleted

Prilikom učitavanja agregata:

1. Učitavaju se svi događaji za dati identifikator.
2. Rekonstruiše se trenutno stanje objekta.
3. Dobija se potpuna istorija promena.

Implementirane komponente:

* EventStore
* AggregateRoot
* LokacijaAggregate
* Snapshot modeli
* StoredEvent
* StoredSnapshot

Prednosti:

* Potpuna istorija promena
* Audit mogućnosti
* Rekonstrukcija stanja sistema
* Lakše otkrivanje problema

---

## Saga Pattern – Orchestration

Implementirana je Saga Orchestration za upravljanje distribuiranim transakcijama.

Tok procesa:

1. Pokreće se kreiranje angažovanja.
2. Saga Orchestrator kreira novu sagu.
3. Vrši se validacija predavača.
4. Vrši se validacija događaja.
5. Nakon uspešnih validacija angažovanje se kreira.
6. Saga se označava kao uspešno završena.

Uloga Orchestratora:

* Koordinacija svih koraka
* Praćenje stanja transakcije
* Upravljanje poslovnim tokom
* Evidentiranje statusa sage

Implementirane komponente:

* SagaOrchestrator
* SagaConsumer
* OutboxDispatcher

---

## Saga Pattern – Choreography

Pored orkestracije implementirana je i Saga Koreografija.

Kod ovog pristupa:

* Ne postoji centralni koordinator.
* Svaki servis reaguje na događaje koji su za njega relevantni.
* Nakon lokalne transakcije servis objavljuje novi događaj.

Implementacije:

* SagaKoreografijaConsumer (Predavač API)
* SagaKoreografijaConsumer (Lokacija API)
* SagaDogadjajKoreografijaConsumer

Prednosti:

* Veća autonomija servisa
* Manje centralne zavisnosti
* Veća skalabilnost

---

## Retry Pattern

Implementiran je Retry mehanizam za privremene greške.

U slučaju neuspele komunikacije:

* Zahtev se automatski ponavlja određeni broj puta.
* Omogućava oporavak od kratkotrajnih problema.
* Smanjuje broj neuspešnih operacija.

Koristi se tokom komunikacije sa eksternim servisima i mikroservisima.

---

## Circuit Breaker Pattern

Implementiran je Circuit Breaker mehanizam.

Funkcionisanje:

* Nakon određenog broja neuspeha prekida se slanje novih zahteva.
* Sistem privremeno odbija pozive ka problematičnom servisu.
* Nakon isteka definisanog vremena pokušava se ponovno uspostavljanje komunikacije.

Prednosti:

* Sprečava kaskadne padove sistema
* Čuva resurse aplikacije
* Povećava stabilnost sistema

Implementirana klasa:

* CircuitBreaker

---

## Timeout Protection

Prilikom komunikacije sa eksternim servisima definisani su timeout mehanizmi.

Na ovaj način:

* Zahtevi ne čekaju beskonačno dugo
* Sprečava se blokiranje aplikacije
* Obezbeđuje se brži oporavak sistema

---

## Dead Letter Queue (DLQ)

Implementirana je Dead Letter Queue podrška za RabbitMQ.

U slučaju da poruka ne može uspešno da se obradi:

1. Poruka se više puta pokušava obraditi.
2. Nakon dostizanja limita prebacuje se u DLQ.
3. Omogućena je kasnija analiza i obrada problematičnih poruka.

Prednosti:

* Ne dolazi do gubitka poruka
* Lakše otkrivanje problema
* Pouzdanija obrada događaja

---

## Idempotentna obrada poruka

Radi sprečavanja duple obrade poruka implementiran je mehanizam idempotentnosti.

Svaka obrađena poruka se evidentira u tabeli:

* ProcessedMessages

Prilikom prijema nove poruke:

1. Proverava se da li je već obrađena.
2. Ako jeste, poruka se ignoriše.
3. Ako nije, vrši se obrada i beleženje.

Prednosti:

* Sprečava dupliranje podataka
* Obezbeđuje konzistentnost sistema
* Povećava pouzdanost distribuiranih transakcija

---

## Pozadinski servisi (Background Services)

Sistem koristi više pozadinskih servisa za obradu asinhronih zadataka.

Implementirani servisi:

* OutboxMessagePublisher
* OutboxDispatcher
* EmailWorker
* RabbitMqConsumerHostedService
* SagaConsumer
* PredavacEventConsumer
* SagaAngazovanjeConsumer

Ovi servisi omogućavaju:

* Obradu poruka
* Slanje email-ova
* Objavu događaja
* Saga koordinaciju
* Sinhronizaciju podataka između servisa

---

## Email Queue Processing

Za obradu email poruka implementiran je sistem redova.

Karakteristike:

* Asinhrono slanje email-ova
* Rate limiting zaštita
* Obrada u pozadini
* Veća otpornost sistema

Implementirane komponente:

* EmailQueueProducer
* EmailWorker

---

## Prednosti implementiranog rešenja

Implementacijom navedenih obrazaca sistem dobija:

* Mikroservisnu arhitekturu
* Visoku dostupnost
* Veću otpornost na greške
* Skalabilnost
* Pouzdanu komunikaciju
* Konzistentnost podataka
* Potpunu istoriju promena
* Jednostavnije održavanje
* Lakše proširivanje sistema
* Profesionalan pristup razvoju distribuiranih aplikacija
