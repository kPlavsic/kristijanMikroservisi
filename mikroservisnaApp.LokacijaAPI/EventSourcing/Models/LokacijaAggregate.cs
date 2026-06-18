using mikroservisnaApp.LokacijaAPI.EventSourcing.Events;

namespace mikroservisnaApp.LokacijaAPI.EventSourcing.Models
{
    public class LokacijaAggregate : AggregateRoot
    {
        public string Naziv { get; private set; } = string.Empty;
        public string Adresa { get; private set; } = string.Empty;
        public int Kapacitet { get; private set; }
        public bool Obrisana { get; private set; } = false;


        public static LokacijaAggregate Kreiraj(int id, string naziv, string adresa, int kapacitet)
        {
            if (string.IsNullOrWhiteSpace(naziv))
                throw new ArgumentException("Naziv lokacije ne sme biti prazan.");
            if (string.IsNullOrWhiteSpace(adresa))
                throw new ArgumentException("Adresa lokacije ne sme biti prazna.");
            if (kapacitet <= 0)
                throw new ArgumentException("Kapacitet mora biti veci od nule.");

            var lokacija = new LokacijaAggregate();

            lokacija.RaiseEvent(new LokacijaKreiranaEvent
            {
                LokacijaId = id,
                Naziv = naziv,
                Adresa = adresa,
                Kapacitet = kapacitet
            });

            return lokacija;
        }

        public void IzmeniNaziv(string noviNaziv)
        {
            if (Obrisana)
                throw new InvalidOperationException("Ne mozete menjati obrisanu lokaciju.");
            if (string.IsNullOrWhiteSpace(noviNaziv))
                throw new ArgumentException("Naziv ne sme biti prazan.");
            if (noviNaziv == Naziv)
                throw new ArgumentException("Novi naziv je isti kao trenutni.");

            RaiseEvent(new NazivPromenjenEvent
            {
                LokacijaId = ID,
                StariNaziv = Naziv,
                NoviNaziv = noviNaziv
            });
        }

        public void IzmeniAdresu(string novaAdresa)
        {
            if (Obrisana)
                throw new InvalidOperationException("Ne mozete menjati obrisanu lokaciju.");
            if (string.IsNullOrWhiteSpace(novaAdresa))
                throw new ArgumentException("Adresa ne sme biti prazna.");
            if (novaAdresa == Adresa)
                throw new ArgumentException("Nova adresa je ista kao trenutna.");

            RaiseEvent(new AdresaPromenjenEvent
            {
                LokacijaId = ID,
                StaraAdresa = Adresa,
                NovaAdresa = novaAdresa
            });
        }

        public void IzmeniKapacitet(int noviKapacitet)
        {
            if (Obrisana)
                throw new InvalidOperationException("Ne mozete menjati obrisanu lokaciju.");
            if (noviKapacitet <= 0)
                throw new ArgumentException("Kapacitet mora biti veci od nule.");
            if (noviKapacitet == Kapacitet)
                throw new ArgumentException("Novi kapacitet je isti kao trenutni.");

            RaiseEvent(new KapacitetPromenjenEvent
            {
                LokacijaId = ID,
                StariKapacitet = Kapacitet,
                NoviKapacitet = noviKapacitet
            });
        }

        public void Obrisi()
        {
            if (Obrisana)
                throw new InvalidOperationException("Lokacija je vec obrisana.");

            RaiseEvent(new LokacijaObrisanaEvent
            {
                LokacijaId = ID
            });
        }

        protected override void Apply(Event @event)
        {
            switch (@event)
            {
                case LokacijaKreiranaEvent e:
                    ID = e.LokacijaId;
                    Naziv = e.Naziv;
                    Adresa = e.Adresa;
                    Kapacitet = e.Kapacitet;
                    Obrisana = false;
                    break;
                case NazivPromenjenEvent e:
                    Naziv = e.NoviNaziv;
                    break;
                case AdresaPromenjenEvent e:
                    Adresa = e.NovaAdresa;
                    break;
                case KapacitetPromenjenEvent e:
                    Kapacitet = e.NoviKapacitet;
                    break;
                case LokacijaObrisanaEvent:
                    Obrisana = true;
                    break;
                default:
                    throw new InvalidOperationException($"Nepoznat tip dogadjaja: {@event.GetType().Name}");
            }
        }

        public override AggregateSnapshot CreateSnapshot()
        {
            return new LokacijaSnapshot
            {
                ID = ID,
                Version = Version,
                Naziv = Naziv,
                Adresa = Adresa,
                Kapacitet = Kapacitet,
                Obrisana = Obrisana
            };
        }

        public override void RestoreSnapshot(AggregateSnapshot snapshot)
        {
            if (snapshot is not LokacijaSnapshot s)
                throw new InvalidOperationException($"Pogresan tip snapshot-a: {snapshot.GetType().Name}");

            ID = s.ID;
            Version = s.Version;
            Naziv = s.Naziv;
            Adresa = s.Adresa;
            Kapacitet = s.Kapacitet;
            Obrisana = s.Obrisana;
        }
    }
}