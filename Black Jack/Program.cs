using System;
using System.Collections.Generic;

namespace Black_Jack
{
    //  KARTE
    class Karte
    {
        public string Symbol { get; }   // z.B. "Herz", "Pik"
        public string Wert { get; }   // z.B. "A", "K", "7"

        public Karte(string symbol, string wert)
        {
            Symbol = symbol;
            Wert = wert;
        }

        // Grundwert der Karte (Ass = 0, wird separat behandelt)
        public int Grundwert()
        {
            return Wert switch
            {
                "A" => 0,   // Ass – separat
                "J" or "Q" or "K" => 10,
                _ => int.Parse(Wert)
            };
        }

        public bool IstAss() => Wert == "A";

        public override string ToString() => $"[{Symbol} {Wert}]";
    }
    //  DECK
    class Deck
    {
        private readonly List<Karte> karten = new();
        private readonly Random rng = new();

        private static readonly string[] Symbole = { "♠", "♥", "♦", "♣" };
        private static readonly string[] Werte = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };

        public Deck()
        {
            foreach (var s in Symbole)
                foreach (var w in Werte)
                    karten.Add(new Karte(s, w));
        }

        public void Mischen()
        {
            for (int i = karten.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (karten[i], karten[j]) = (karten[j], karten[i]);
            }
            Console.WriteLine("🂠  Deck wurde gemischt.\n");
        }

        public Karte Ziehen()
        {
            if (karten.Count == 0)
                throw new InvalidOperationException("Das Deck ist leer!");
            var k = karten[0];
            karten.RemoveAt(0);
            return k;
        }
    }
    //  HAND  (Spieler oder Dealer)
    class Hand
    {
        private readonly List<Karte> karten = new();
        private readonly List<int> assWerte = new();   // 1 oder 11 pro Ass

        public IReadOnlyList<Karte> Karten => karten;
        public int Punkte { get; private set; }

        // Karte hinzufügen; isDealer = kein interaktiver Ass-Dialog
        public void KarteHinzufuegen(Karte k, bool isDealer = false)
        {
            karten.Add(k);

            if (k.IstAss())
            {
                int assWahl = AssWaehlen(isDealer);
                assWerte.Add(assWahl);
                Punkte += assWahl;
            }
            else
            {
                Punkte += k.Grundwert();
            }
        }

        // Ass-Logik
        private int AssWaehlen(bool isDealer)
        {
            if (isDealer)
            {
                // Dealer: 11 wenn ≤ 10, sonst 1
                int wahl = (Punkte + 11 <= 21) ? 11 : 1;
                Console.WriteLine($"  Dealer wählt Ass = {wahl}");
                return wahl;
            }

            // Spieler: Punktestand VOR dem Ass
            int vorAss = Punkte;

            Console.WriteLine($"\n  ♠ Du hast ein Ass gezogen! Aktueller Punktestand (ohne Ass): {vorAss}");

            // Fall: 11 würde > 21 ergeben  →  nur 1 erlaubt
            if (vorAss + 11 > 21)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("  ERROR 11 – Die 11 würde deinen Punktestand über 21 bringen!");
                Console.ResetColor();
                Console.WriteLine("  Das Ass wird automatisch als 1 gewertet.");
                return 1;
            }

            // Normaler Dialog
            while (true)
            {
                Console.Write("  Ass als [1] oder [11] zählen? Eingabe: ");
                string eingabe = Console.ReadLine()?.Trim() ?? "";

                if (eingabe == "1")
                {
                    Console.WriteLine("  Ass wird als 1 gewertet.");
                    return 1;
                }
                if (eingabe == "11")
                {
                    Console.WriteLine("  Ass wird als 11 gewertet.");
                    return 11;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  Ungültige Eingabe – bitte 1 oder 11 eingeben.");
                Console.ResetColor();
            }
        }

        public bool IstUeber21() => Punkte > 21;
        public bool IstBlackjack() => karten.Count == 2 && Punkte == 21;

        public void Anzeigen(string name, bool versteckeZweite = false)
        {
            Console.Write($"  {name,-10}: ");
            for (int i = 0; i < karten.Count; i++)
            {
                if (i == 1 && versteckeZweite)
                    Console.Write("[🂠 verdeckt] ");
                else
                    Console.Write(karten[i] + " ");
            }
            if (!versteckeZweite)
                Console.Write($"  → {Punkte} Punkte");
            Console.WriteLine();
        }
    }

    //  SPIEL
    class Spiel
    {
        private Deck deck = new();
        private Hand spieler = new();
        private Hand dealer = new();
        private int guthaben = 1000;

        public void Starten()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Titelbildschirm();

            while (true)
            {
                Console.Clear();
                Titelbildschirm();
                Console.WriteLine($"  💰 Guthaben: {guthaben} €\n");

                int einsatz = EinsatzAbfragen();
                if (einsatz == 0) break;

                Runde(einsatz);

                Console.WriteLine("\n  Nochmal spielen? [J/N]");
                if (Console.ReadLine()?.Trim().ToUpper() != "J") break;
            }

            Console.WriteLine($"\n  Spiel beendet. Endguthaben: {guthaben} €\n");
        }

        // ── Eine Runde 
        private void Runde(int einsatz)
        {
            deck = new Deck();
            spieler = new Hand();
            dealer = new Hand();
            deck.Mischen();

            // Austeilen
            spieler.KarteHinzufuegen(deck.Ziehen());
            dealer.KarteHinzufuegen(deck.Ziehen(), isDealer: true);
            spieler.KarteHinzufuegen(deck.Ziehen());
            dealer.KarteHinzufuegen(deck.Ziehen(), isDealer: true);

            // Anzeige
            Console.WriteLine();
            dealer.Anzeigen("Dealer", versteckeZweite: true);
            spieler.Anzeigen("Du");

            // Sofort-Blackjack?
            if (spieler.IstBlackjack())
            {
                Console.WriteLine("\n  🎉 BLACKJACK! Du gewinnst!");
                guthaben += (int)(einsatz * 1.5);
                return;
            }

            // Spieler-Zug
            while (true)
            {
                Console.Write("\n  [H]it / [S]tand? ");
                string wahl = Console.ReadLine()?.Trim().ToUpper() ?? "";

                if (wahl == "H")
                {
                    spieler.KarteHinzufuegen(deck.Ziehen());
                    Console.WriteLine();
                    dealer.Anzeigen("Dealer", versteckeZweite: true);
                    spieler.Anzeigen("Du");

                    if (spieler.IstUeber21())
                    {
                        Console.WriteLine("\n  💥 Bust! Du hast über 21 – Verloren.");
                        guthaben -= einsatz;
                        return;
                    }
                }
                else if (wahl == "S")
                {
                    break;
                }
                else
                {
                    Console.WriteLine("  Ungültige Eingabe.");
                }
            }

            // Dealer-Zug (zieht bis ≥ 17)
            Console.WriteLine("\n  Dealer deckt auf:");
            dealer.Anzeigen("Dealer");
            while (dealer.Punkte < 17)
            {
                Console.WriteLine("  Dealer zieht...");
                dealer.KarteHinzufuegen(deck.Ziehen(), isDealer: true);
                dealer.Anzeigen("Dealer");
            }

            // Ergebnis
            Console.WriteLine();
            spieler.Anzeigen("Du");
            dealer.Anzeigen("Dealer");

            if (dealer.IstUeber21())
            {
                Console.WriteLine("\n  🎉 Dealer ist gebusted – über 21 – Du gewinnst!");
                guthaben += einsatz;
            }
            else if (spieler.Punkte > dealer.Punkte)
            {
                Console.WriteLine("\n  🎉 Du gewinnst!");
                guthaben += einsatz;
            }
            else if (spieler.Punkte < dealer.Punkte)
            {
                Console.WriteLine("\n  😞 Verloren.");
                guthaben -= einsatz;
            }
            else
            {
                Console.WriteLine("\n  🤝 Unentschieden – Einsatz zurück.");
            }
        }

        // ── Hilfsfunktionen ───────────────────────────
        private int EinsatzAbfragen()
        {
            while (true)
            {
                Console.Write("  Einsatz (0 = Beenden, max. " + guthaben + " €): ");
                if (int.TryParse(Console.ReadLine(), out int e))
                {
                    if (e == 0) return 0;
                    if (e > 0 && e <= guthaben) return e;
                }
                Console.WriteLine("  Ungültiger Einsatz.");
            }
        }

        private static void Titelbildschirm()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
  ╔══════════════════════════════════════╗
  ║         ♠ B L A C K J A C K ♠        ║
  ╚══════════════════════════════════════╝");
            Console.ResetColor();
        }
    }

    // ══════════════════════════════════════════════════
    //  EINSTIEGSPUNKT
    // ══════════════════════════════════════════════════
    internal class Program
    {
        static void Main(string[] args) => new Spiel().Starten();
    }
}
    

