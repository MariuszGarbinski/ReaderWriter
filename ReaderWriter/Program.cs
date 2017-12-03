using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReaderWriter
{
    class Program
    {
        static Random r = new Random();
        const int ileElementow = 10;
        static int[] tablica = new int[ileElementow];

        const int ileWatkowPisarzy = 2;
        const int ileWatkowCzytelnikow = 10;
        const int maksymalnaPrzerwaMiedzyOdczytami = 10000; //10 sekund
        const int maksymalnaPrzerwaMiedzyModyfikacjami = 10000; //10 sekund
        const int maksymalnaDlugoscOdczytu = 1000; //1 sekunda
        const int maksymalnaDlugoscModyfikacji = 1000; //1 sekunda
        static ReaderWriterLockSlim rwls = new ReaderWriterLockSlim();

        static void modyfikujElement(int indeks, int? wartosc = null)
        {
            rwls.EnterWriteLock();
            Console.WriteLine("Wątki czekające na zapis: {0}, wątki czekające na odczyt {1}", rwls.WaitingWriteCount, rwls.WaitingReadCount);

            try
            {
                if (wartosc.HasValue) tablica[indeks] = wartosc.Value;
                else tablica[indeks]++;
                Console.WriteLine("Element " + indeks.ToString() + " został zmieniony w wątku nr " + Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(r.Next(maksymalnaDlugoscModyfikacji));
            }
            catch (Exception exc)
            {
                Console.WriteLine("Modyfikacja elementu " + indeks.ToString() + " w wątku " + Thread.CurrentThread.ManagedThreadId + " nie jest możliwa (" + exc.Message + ")");
            }
            finally
            {
                rwls.ExitWriteLock();
            }
        }

        static int odcztajElement(int indeks)
        {
            int wynik = -1;
            rwls.EnterReadLock();
            Console.WriteLine("Wątki równocześnie odczytujące: {0}. Wątki czekające na zapis: {1}", rwls.CurrentReadCount, rwls.WaitingWriteCount);

            try
            {
                wynik = tablica[indeks];
                Console.WriteLine("Element " + indeks.ToString() + " równy jest \"" + wynik.ToString() + "\"");
                Thread.Sleep(r.Next(maksymalnaDlugoscOdczytu));
                return wynik;
            }
            catch (Exception exc)
            {
                Console.WriteLine("Odczyt elementu " + indeks.ToString() + " nie jest możliwy (" + exc.Message + ")");
                return wynik;
            }
            finally
            {
                rwls.ExitReadLock();
            }
        }

        private static void wyswietlZawartoscTablicy()
        {
            Console.WriteLine("Zawartość tablicy:");
            foreach (int element in tablica) Console.Write(element.ToString() + "\n");
            Console.WriteLine("[Koniec]");
        }

        static void Main(string[] args)
        {
            wyswietlZawartoscTablicy();
            Console.WriteLine("Naciśnij Enter...");
            Console.WriteLine("Następnie naciśnij Enter, jeżeli będziesz chciał zakończyć program.");
            Console.ReadLine();

            ThreadStart akcjaPisarza =
                () =>
                {
                    Thread.Sleep(r.Next(maksymalnaPrzerwaMiedzyModyfikacjami));
                    //opóźnienie
                    while (true)
                    {
                        try
                        {
                            Console.WriteLine("Przygotowania do modyfikacji elementu (wątek nr " + Thread.CurrentThread.ManagedThreadId + ")");
                            int indeks = r.Next(ileElementow);
                            modyfikujElement(indeks);
                        }
                        catch (ThreadAbortException)
                        {
                            Console.WriteLine("Wątek pisarza " + Thread.CurrentThread.ManagedThreadId + " kończy pracę");
                        }
                    }
                };
            ThreadStart akcjaCzytelnika =
                () =>
                {
                    Thread.Sleep(r.Next(maksymalnaPrzerwaMiedzyOdczytami));
                    //opóźnienie
                    while (true)
                    {
                        try
                        {
                            Console.WriteLine("Przygotowania do odczytania elementu (wątek nr " + Thread.CurrentThread.ManagedThreadId + ")");
                            int indeks = r.Next(ileElementow);
                            int wartoscElementu = odcztajElement(indeks);
                            Console.WriteLine("Odczytany element o indeksie " + indeks.ToString() + " równy jest " + wartoscElementu + " (wątek nr " + Thread.CurrentThread.ManagedThreadId + ")");
                            Thread.Sleep(maksymalnaPrzerwaMiedzyOdczytami);
                        }
                        catch (ThreadAbortException)
                        {
                            Console.WriteLine("Wątek czytelnika " + Thread.CurrentThread.ManagedThreadId + " kończy pracę");
                        }
                    }
                };

            Thread[] pisarze = new Thread[ileWatkowPisarzy];
            for(int i = 0; i < ileWatkowPisarzy; ++i)
            {
                pisarze[i] = new Thread(akcjaPisarza);
                //pisarze[i].Priority = ThreadPriority.AboveNormal;
                pisarze[i].IsBackground = true;
                pisarze[i].Start();
            }

            Thread[] czytelnicy = new Thread[ileWatkowCzytelnikow];
            for(int i = 0; i < ileWatkowCzytelnikow; ++i)
            {
                czytelnicy[i] = new Thread(akcjaCzytelnika);
                czytelnicy[i].IsBackground = true;
                czytelnicy[i].Start();
            }

            Console.ReadLine();
            Console.WriteLine("\nKończenie pracy programu...");
            for (int i = 0; i < ileWatkowPisarzy; ++i) pisarze[i].Abort();
            for (int i = 0; i < ileWatkowCzytelnikow; ++i) czytelnicy[i].Abort();

            wyswietlZawartoscTablicy();

            Console.ReadKey();
        }
    }
}