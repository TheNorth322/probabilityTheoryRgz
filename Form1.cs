using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test
{
    public partial class Form1 : Form
    {
        int N = 1000; // кол-во сгенерированных чисел
        int interval_amount = 20; // кол-во интервалов
        double m = 0.6; // данные для интервала
        double sigma = 0.2;
        public Form1()
        {
            InitializeComponent();
        }
        private void button1_Click_1(object sender, EventArgs e)
        {
            Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application();

            double[] x = new double[N]; // массив значений выборки
            Random r = new Random(); // генератор случайных значений

            double k = Convert.ToDouble(textBox1.Text); // получение k из textBox1

            for (int i = 0; i < N; i++) // заполнение массива выборки
            {
                x[i] = 0;
                
                for (int j = 0; j < interval_amount; j++)
                {
                    double randX = r.NextDouble(); // генерируем число в диапазоне m - sigma*k - offset до m - sigma*k + offset.
                    if (randX >= m - sigma * k && randX < m + sigma * k) x[i] += randX; // offset нужен осуществе того, чтобы часть чисел попала в интерал, а часть нет
                }
            }
            double max = x.Max(), min = x.Min(); // мин и макс значения в выборке
            double interval_len = (max - min) / interval_amount; // длина интервала
            
            double[] middles = new double[interval_amount]; // середины интервалов
            double[] frequencies = new double[interval_amount]; // частоты попаданий значений
            double[] n = new double[interval_amount]; // значения нормальной кривой

            for (int i = 0; i < interval_amount; i++) // заполнение массивов середин интервалов и частот
            {
                int amount_in_interval = 0;
                double left_border = min + interval_len * i, right_border = min + interval_len * (i + 1);
          
                for (int j = 0; j < x.Length; j++)
                    if (x[j] >= left_border && x[j] < right_border) amount_in_interval++;

                frequencies[i] = amount_in_interval / (interval_len);
                middles[i] = (left_border + right_border) / 2;
            }

            double avg = average(middles, frequencies); // выбор. среднее
            double avg_square_deviation = deviation(middles, frequencies, avg); // среднее квадр отклонение
            double chi_square_seen = 0, q = interval_amount - 2 - 1, chi_square_crit, freqSum = excel.WorksheetFunction.Sum(frequencies);

            chi_square_crit = excel.WorksheetFunction.ChiInv(0.05, q); // считаем хи квадрат крит

            // Считаем точки для нормальной кривой, а также хи квадрат наблюдаемое
            for (int i = 0; i < interval_amount; i++)
            {
                n[i] = interval_len * freqSum / avg_square_deviation * excel.WorksheetFunction.NormDist(((middles[i] - avg) / avg_square_deviation), 0, 1, false);
                chi_square_seen += (((frequencies[i] - n[i]) * (frequencies[i] - n[i])) / n[i]);
            }
            // вывод данных на экран в поля textBox(2-5)
            textBox2.Text = avg.ToString();
            textBox3.Text = avg_square_deviation.ToString();
            textBox4.Text = chi_square_seen.ToString();
            textBox5.Text = chi_square_crit.ToString();
            // Проверка критерия Пирсона
            if (chi_square_seen > chi_square_crit) label7.Text = "Согласно критерию Пирсона генеральная совокупность не распределена нормально";
            else label7.Text = "Согласно критерию Пирсона генеральная совокупность распределена нормально";
            Build(x, n);
        }
        double deviation(double[] mid, double[] freq, double avg) // ср. квадр. откл
        {
            Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application();
            double dev = 0;
            for (int i = 0; i < mid.Length; i++)
                dev += mid[i] * mid[i] * freq[i];
            dev = Math.Sqrt((dev / excel.WorksheetFunction.Sum(freq)) - avg*avg);

            return dev;
        }
        double average(double[] mid, double[] freq) // выбор. среднее
        {
            Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application();
            double avg = 0;
            for (int i = 0; i < mid.Length; i++)
                avg += mid[i]*freq[i];
            avg /= excel.WorksheetFunction.Sum(freq);

            return avg;
        }
        void Build(double[] hist_arr, double[] y) // построение гистограммы и нормальной кривой 
        {
            chart1.Series[0].Points.Clear(); // очищаем гистограмму
            chart1.Series[1].Points.Clear();

            double max = hist_arr.Max(), min = hist_arr.Min(); // макс и мин значения выборки
            double interval_len = (max - min) / interval_amount; // длина интервала

            for (int i = 0; i < interval_amount; i++) // построение гистограммы
            {
                int amount_in_interval = 0;
                double left_border = min + interval_len * i, right_border = min + interval_len * (i + 1); // границы интервала i
                for (int j = 0; j < N; j++)
                    if (hist_arr[j] >= left_border && hist_arr[j] < right_border) amount_in_interval++; // кол-во попаданий в диапазон i

                chart1.Series[0].Points.AddXY(i, amount_in_interval / (interval_len * hist_arr.Length)); // (interval_len * N)
                chart1.Series[1].Points.AddXY(i, y[i] / hist_arr.Length); // y[i] / N
            }
        }
    }
}
