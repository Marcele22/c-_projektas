using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

class Program
{
    static Dictionary<string, Dictionary<string, int>> zodziuIndeksas = new Dictionary<string, Dictionary<string, int>>();

    static void Main(string[] args)
    {
        // Nustatome procesoriaus branduolį
        Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1); // Pirmas branduolys ScannerA, antras ScannerB

        if (args.Length < 2)
        {
            Console.WriteLine("Naudojimas: Skaneris <katalogas> <vamzdzio_pavadinimas>");
            return;
        }

        string katalogas = args[0];
        string vamzdzioPavadinimas = args[1];

        // Sukuriame dvi gijas: viena failų skaitymui, kita duomenų siuntimui
        Thread skaitymoGija = new Thread(() => SkaitytiFailus(katalogas));
        Thread siuntimoGija = new Thread(() => SiustiDuomenis(vamzdzioPavadinimas));

        skaitymoGija.Start();
        siuntimoGija.Start();

        skaitymoGija.Join();
        siuntimoGija.Join();
    }

    static void SkaitytiFailus(string katalogas)
    {
        try
        {
            foreach (string failas in Directory.GetFiles(katalogas, "*.txt"))
            {
                string failoPavadinimas = Path.GetFileName(failas);
                Dictionary<string, int> zodziuSkaicius = new Dictionary<string, int>();

                string turinys = File.ReadAllText(failas);
                string[] zodziai = Regex.Split(turinys.ToLower(), @"\W+");

                foreach (string zodis in zodziai)
                {
                    if (!string.IsNullOrWhiteSpace(zodis))
                    {
                        if (zodziuSkaicius.ContainsKey(zodis))
                            zodziuSkaicius[zodis]++;
                        else
                            zodziuSkaicius[zodis] = 1;
                    }
                }

                lock (zodziuIndeksas)
                {
                    zodziuIndeksas[failoPavadinimas] = zodziuSkaicius;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Klaida skaitant failus: {ex.Message}");
        }
    }

    static void SiustiDuomenis(string vamzdzioPavadinimas)
    {
        try
        {
            using (NamedPipeClientStream vamzdis = new NamedPipeClientStream(".", vamzdzioPavadinimas, PipeDirection.Out))
            {
                vamzdis.Connect();
                using (StreamWriter rasytojas = new StreamWriter(vamzdis) { AutoFlush = true })
                {
                    lock (zodziuIndeksas)
                    {
                        foreach (var failas in zodziuIndeksas)
                        {
                            foreach (var zodis in failas.Value)
                            {
                                string eilute = $"{failas.Key}:{zodis.Key}:{zodis.Value}";
                                rasytojas.WriteLine(eilute);
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Klaida siunčiant duomenis: {ex.Message}");
        }
    }
}