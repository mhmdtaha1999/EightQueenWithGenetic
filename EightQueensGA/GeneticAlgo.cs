using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;

namespace EightQueensGA
{
    struct Chromosome
    {
        public int[] genes;
        public int fitness;
        public double cumAvgFitness; //cumilative of average of fitness value
    }

   
    delegate void Progress(int progress);
    class GeneticAlgo
    {
       
        public event Progress progress;
        private const int MAX_FIT = 28;
        private Random random;        

        public GeneticAlgo()
        {
            random = new Random((int)DateTime.Now.Ticks);
        }

        public void DoMating(ref List<Chromosome> initPopulation, int generations, double probCrossver, double probMutation,double elitismRate,Chart chart, ref List<Chromosome> bestChromosome)
        {


            List<double> averageFitnessInEveryGen = new List<double>();
            List<Chromosome> newGeneration;
            int totalFitness = 0;
            
            //CalcFitness(ref initPopulation, ref totalFitness);            

            for (int generation = 0; generation < generations; generation++)
            {

                // اجرای محاسبه تناسب
                CalcFitness(ref initPopulation, ref totalFitness, ref bestChromosome);

                // مرتب‌سازی بر اساس تناسب برای پیدا کردن نخبه‌ها
                initPopulation = initPopulation.OrderByDescending(chromo => chromo.fitness).ToList();

                var eliteCount = (int)(initPopulation.Count * elitismRate);
                List<Chromosome> elites = initPopulation.Take(eliteCount).ToList();

                // ایجاد نسل جدید
                newGeneration = new List<Chromosome>(initPopulation);

                // اضافه کردن نخبه‌ها به نسل جدید
                newGeneration.InsertRange(0, elites);

                // اطمینان از اینکه اندازه جمعیت ثابت باقی می‌ماند
                if (newGeneration.Count > initPopulation.Count)
                {
                    newGeneration = newGeneration.GetRange(0, initPopulation.Count);
                }

                // بروزرسانی جمعیت با نسل جدید
                initPopulation = newGeneration;

                PrepareRuletteWheel(ref initPopulation,totalFitness);
                Crossover(ref initPopulation, probCrossver);
                Mutate(ref initPopulation, probMutation);
                CalcFitness(ref initPopulation, ref totalFitness,ref bestChromosome);
                averageFitnessInEveryGen.Add(totalFitness/initPopulation.Count);
                if (initPopulation[initPopulation.Count - 1].fitness == 28)
                    break;
                if (progress != null)
                {
                    progress(generation + 1);
                }

                UpdateChart(chart,generation,averageFitnessInEveryGen);
               
            }
            initPopulation.Sort(new FitnessComparator());



            
          
        }

        private void UpdateChart(Chart chart, int generation, List<double> averageFitnessInEveryGen)
        {
            chart.Series.Clear();
            Series series = new Series
            {
                Name = "AverageFitness",
                ChartType = SeriesChartType.Line
            };

            for (int i = 0; i <= generation; i++)
            {
                series.Points.AddXY(i, averageFitnessInEveryGen[i]);
            }

            chart.Series.Add(series);
            chart.ChartAreas[0].AxisX.Title = "Generation";
            chart.ChartAreas[0].AxisY.Title = "Average Fitness";
            chart.ChartAreas[0].AxisX.Minimum = 0;
            chart.ChartAreas[0].AxisX.Maximum = generation;
            chart.ChartAreas[0].AxisY.Maximum = 28;
            chart.ChartAreas[0].AxisY.Minimum = 0;
        }

        public void Crossover(ref List<Chromosome> parents, double probability)
        {
            List<Chromosome> newpopulation = new List<Chromosome>();

            for (int i = 0; i < parents.Count; i++)
            {                
                if (Assay(probability)) //if the chance is to crossover
                {
                    Chromosome parentX = AssayRuletteWheel(parents);
                    Chromosome parentY = AssayRuletteWheel(parents);

                    List<int> child = new List<int>();
                    for (int j = 0; j < 8; j++)
                    {
                        if (Assay(0.5)) //select from parentX
                        {
                            for (int k = 0; k < parentX.genes.Length; k++) 
                            {
                                if (!child.Contains(parentX.genes[k]))//instead of deleting the similar genes from parents select the next non-contained number
                                {
                                    child.Add(parentX.genes[k]);
                                    break;
                                }
                            }
                        }
                        else //select from parentY
                        {
                            for (int k = 0; k < parentY.genes.Length; k++)
                            {
                                if (!child.Contains(parentY.genes[k]))//instead of deleting the similar genes from parents select the next non-contained number
                                {
                                    child.Add(parentY.genes[k]);
                                    break;
                                }
                            }
                        }
                    }
                    Chromosome nPop = new Chromosome();
                    nPop.genes = child.ToArray();
                    newpopulation.Add(nPop);

                }
                else //else the chance is to clonning
                {
                    Chromosome parentX = AssayRuletteWheel(parents);
                    newpopulation.Add(parentX);
                }
            }

            while (newpopulation.Count > parents.Count)
            {
                newpopulation.RemoveAt((int)GetRandomVal(0, newpopulation.Count - 1));
            }

            parents = newpopulation;
        }


        private void PrepareRuletteWheel(ref List<Chromosome> parents,int total)
        {
            int currentTotalFitness=0;
            for (int i = 0; i < parents.Count; i++)
            {
                currentTotalFitness += parents[i].fitness;
                Chromosome temp = parents[i];
                temp.cumAvgFitness = currentTotalFitness / (double)total;
                parents[i] = temp;
            }
        }

        private Chromosome AssayRuletteWheel(List<Chromosome> parents)
        {
            Chromosome selection = parents[0];
            double probability = random.NextDouble();
            for (int i = 0; i < parents.Count; i++)
            {
                selection = parents[i];
                if (parents[i].cumAvgFitness > probability)
                    break;
                
            }
            return selection;
        }

        public void Mutate(ref List<Chromosome> parents, double probability)
        {
            List<Chromosome> offsprings = new List<Chromosome>();   

            for (int i = 0; i < parents.Count; i++)
            {
                Chromosome offspring = parents[i];
                for (int mutatePosition = 0; mutatePosition < 8; mutatePosition++)
                {
                    if (Assay(probability)) //if the chance is to mutate
                    {
                        int newGeneIndex = (int)(GetRandomVal(0,6)+0.5);
                        if (newGeneIndex>=mutatePosition)
                        {
                            newGeneIndex += 1;
                        }
                        int swapTemp = offspring.genes[mutatePosition];
                        offspring.genes[mutatePosition] = offspring.genes[newGeneIndex];
                        offspring.genes[newGeneIndex] = swapTemp;                       
                    }
                }                                                       
                
                offsprings.Add(offspring);                
            }

            parents = offsprings;
        }

        public void Elitism(ref List<Chromosome> parents, double probability)
        {

        }

        public double GetRandomVal(double min, double max)
        {
            return min + random.NextDouble() * (max - min);            
        }

        private bool Assay(double probability)
        {
            if (random.NextDouble() < probability)
                return true;
            else
                return false;
        }

        public void CalcFitness(ref List<Chromosome> chromosome, ref int totalFitness , ref List<Chromosome> bestChromosome)
        {
            
            int collisions = 0;
            totalFitness = 0;
            Chromosome tempBest = new Chromosome { fitness = int.MinValue, genes = new int[8] };
            tempBest.fitness = 0;
            for (int k = 0; k < chromosome.Count; k++)
            {
                for (int i = 0; i < chromosome[k].genes.Length - 1; i++)
                {
                    int x = i;
                    int y = chromosome[k].genes[i];
                    for (int j = i + 1; j < chromosome[k].genes.Length; j++)
                    {
                        if (Math.Abs(j - x) == Math.Abs(chromosome[k].genes[j] - y))
                            collisions++;                        
                    }
                }
                
                Chromosome temp = chromosome[k];
                temp.fitness = MAX_FIT - collisions;
                if(tempBest.fitness < temp.fitness)
                {
                    tempBest.fitness = temp.fitness;
                    tempBest.genes = temp.genes;
                }
                chromosome[k] = temp;
                totalFitness += chromosome[k].fitness;
                collisions = 0;
            }

            bestChromosome.Add(tempBest);
        }
    }

    class FitnessComparator : Comparer<Chromosome>
    {
        public override int Compare(Chromosome x, Chromosome y)
        {
            if (x.fitness == y.fitness)
                return 0;
            else if (x.fitness < y.fitness)
                return 1;
            else
                return -1;
        }
    }
}
