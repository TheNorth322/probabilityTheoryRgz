using System;
using System.Linq;
using System.Windows.Forms;

namespace Test
{
    public partial class Form1 : Form
    {
        // Объем выборки
        public static int N = 1000;
        // Мат. ожидание
        double M = 0.5;
        // Количество интервалов 
        int INTERVAL_AMOUNT = (int)(1 + Math.Floor(Math.Log(N, 2)));
        // Среднее квадратическое отклонение
        double AVG_SQUARE_DEVIATION = 0.16;

        public class InvalidValueException : Exception 
        {
            public InvalidValueException() : base() { }
            public InvalidValueException(string message) : base(message) { }
            public InvalidValueException(string message, Exception inner) : base(message, inner) { }
            protected InvalidValueException(System.Runtime.Serialization.SerializationInfo info,
                System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application();
            // Массив значений выборки
            double[] x = new double[N]; 
            Random r = new Random();
            double k = 0;

            // Получение k из textBox1
            try
            {
                k = Convert.ToDouble(textBox1.Text);
                if (k <= 0)
                    throw new InvalidValueException("Invalid value of k, must be greater than 0");
            }
            catch
            {
                MessageBox.Show("Неверное значение k, должно быть больше 0", "Ошибка! Неверное значение",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            chart2.Series[0].Points.Clear();
            chart2.Series[1].Points.Clear();

            for (int i = 0; i < N; i++)
            {
                while (true)
                {
                    double randX = r.NextDouble();

                    if (randX < M - AVG_SQUARE_DEVIATION * k || randX > M + AVG_SQUARE_DEVIATION * k) continue;

                    double randY = r.NextDouble() * (2.5 - 0) + 0;
                    double theorY = Math.Exp(-(((randX - M) * (randX - M)) / (2 * AVG_SQUARE_DEVIATION * AVG_SQUARE_DEVIATION)))
                        / (AVG_SQUARE_DEVIATION * Math.Sqrt(2 * Math.PI));

                    if (randY <= theorY)
                    {
                        x[i] = randX;
                        chart2.Series[0].Points.AddXY(randX, theorY);
                        chart2.Series[1].Points.AddXY(randX, randY);
                        break;
                    }
                }
            }
            
            double max = x.Max(), min = x.Min();
            
            // Длина интервала
            double intervalLen = (max - min) / INTERVAL_AMOUNT;
            
            // Середины интервалов
            double[] middles = new double[INTERVAL_AMOUNT];

            // Частоты попаданий значений в интервалы
            double[] frequencies = new double[INTERVAL_AMOUNT];

            // Значения нормальной кривой
            double[] n = new double[INTERVAL_AMOUNT];

            // Заполнение массивов середин интервалов и частот
            for (int i = 0; i < INTERVAL_AMOUNT; i++) 
            {
                int amountInInterval = 0;
                double leftBorder = min + intervalLen * i;
                double rightBorder = min + intervalLen * (i + 1);
          
                for (int j = 0; j < x.Length; j++)
                    if (x[j] >= leftBorder && x[j] < rightBorder) amountInInterval++;

                frequencies[i] = amountInInterval;
                middles[i] = (leftBorder + rightBorder) / 2;
            }

            // Выборочное среднее
            double sampleMean = getSampleMean(middles, frequencies);

            // Хи квадрат наблюдаемое, кол-во степеней свободы
            double chiSquareSeen = 0, degree = INTERVAL_AMOUNT - 2 - 1;
            
            // Сумма частот
            double freqSum = excel.WorksheetFunction.Sum(frequencies);

            // Хи квадрат критическое
            double chiSquareCrit = excel.WorksheetFunction.ChiInv(0.05, degree);

            // Считаем точки для нормальной кривой, а также хи квадрат наблюдаемое
            for (int i = 0; i < INTERVAL_AMOUNT; i++)
            {
                // Значение функции плотности нормального распределения
                double t = excel.WorksheetFunction.NormDist((middles[i] - sampleMean) / AVG_SQUARE_DEVIATION, 0, 1, false);

                // Теоретические частоты
                n[i] = intervalLen * freqSum / AVG_SQUARE_DEVIATION * t;
                chiSquareSeen += (((frequencies[i] - n[i]) * (frequencies[i] - n[i])) / n[i]);
            }
            
            // вывод данных на экран в поля textBox(2-5)
            textBox2.Text = sampleMean.ToString();
            textBox4.Text = chiSquareSeen.ToString();
            textBox5.Text = chiSquareCrit.ToString();
            
            // Проверка критерия Пирсона
            if (chiSquareSeen > chiSquareCrit) 
                label7.Text = "Согласно критерию Пирсона генеральная совокупность не распределена нормально";
            else 
                label7.Text = "Согласно критерию Пирсона генеральная совокупность распределена нормально";

            excel.Quit();
            buildHist(frequencies, n, intervalLen);
            
        }
        double getSampleMean(double[] mid, double[] freq) // выбор. среднее
        {
            Microsoft.Office.Interop.Excel.Application excel = new Microsoft.Office.Interop.Excel.Application();
            double sampleMean = 0;
            for (int i = 0; i < mid.Length; i++)
                sampleMean += mid[i]*freq[i];
            sampleMean /= excel.WorksheetFunction.Sum(freq);

            return sampleMean;
        }

        // Построение гистограммы и нормальной кривой
        void buildHist(double[] histFreq, double[] normCurve, double intervalLen)
        {
            // Очищаем гистограмму
            chart1.Series[0].Points.Clear(); 
            chart1.Series[1].Points.Clear();
            // Построение гистограммы
            for (int i = 0; i < INTERVAL_AMOUNT; i++)
            {
               chart1.Series[0].Points.AddXY(i, histFreq[i] / intervalLen);
               chart1.Series[1].Points.AddXY(i, normCurve[i] / intervalLen);
            }
        }
    }
}
