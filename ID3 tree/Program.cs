using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Data;
using System.IO;

namespace ID3_tree
{
    class Program
    {
       
       
        public class Attribute
        {
            ArrayList mValues;
            string mName;
            object mLabel;
    
            public Attribute(string name, string[] values)
            {
                mName = name;
                mValues = new ArrayList(values);
                mValues.Sort();
            }

            public Attribute(object Label)
            {
                mLabel = Label;
                mName = string.Empty;
                mValues = null;
            }

         
            public string AttributeName
            {
                get
                {
                    return mName;
                }
            }
          
            public string[] values
            {
                get
                {
                    if (mValues != null)
                        return (string[])mValues.ToArray(typeof(string));
                    else
                        return null;
                }
            }

         
            public bool isValidValue(string value)
            {
                return indexValue(value) >= 0;
            }

          
            public int indexValue(string value)
            {
                if (mValues != null)
                    return mValues.BinarySearch(value);
                else
                    return -1;
            }
                                     
            public override string ToString()
            {
                if (mName != string.Empty)
                {
                    return mName;
                }
                else
                {
                    return mLabel!=null ? mLabel.ToString() : "False";
                }
            }
        }
                             
        public class TreeNode
        {
            private ArrayList mChilds = null;
            private Attribute mAttribute;

        
            public TreeNode(Attribute attribute)
            {
                if (attribute.values != null)
                {
                    mChilds = new ArrayList(attribute.values.Length);
                    for (int i = 0; i < attribute.values.Length; i++)
                        mChilds.Add(null);
                }
                else
                {
                    mChilds = new ArrayList(1);
                    mChilds.Add(null);
                }
                mAttribute = attribute;
            }

            
            public void AddTreeNode(TreeNode treeNode, string ValueName)
            {
                int index = mAttribute.indexValue(ValueName);
                mChilds[index] = treeNode;
            }

          
            public int totalChilds
            {
                get
                {
                    return mChilds.Count;
                }
            }
        
            public TreeNode getChild(int index)
            {
                return (TreeNode)mChilds[index];
            }
           
            public Attribute attribute
            {
                get
                {
                    return mAttribute;
                }
            }

      
            public TreeNode getChildByBranchName(string branchName)
            {
                int index = mAttribute.indexValue(branchName);
                return (TreeNode)mChilds[index];
            }
        }


        public class DecisionTreeID3
        {
            private DataTable mSamples;
            private int mTotalPositives = 0;
            private int mTotal = 0;
            private string mTargetAttribute = "result";
            private double mEntropySet = 0.0;

     
            private int countTotalPositives(DataTable samples)
            {
                int result = 0;

                foreach (DataRow aRow in samples.Rows)
                {
                    if ((bool)aRow[mTargetAttribute] == true)
                        result++;
                }

                return result;
            }
    
            private double calcEntropy(int positives, int negatives)
            {
                int total = positives + negatives;
                double ratioPositive = (double)positives/total;
                double ratioNegative = (double)negatives/total;

                if (ratioPositive != 0)
                    ratioPositive = -(ratioPositive) * System.Math.Log(ratioPositive, 2);
                if (ratioNegative != 0)
                    ratioNegative = -(ratioNegative) * System.Math.Log(ratioNegative, 2);

                double result =  ratioPositive + ratioNegative;

                return result;
            }

         
            private void getValuesToAttribute(DataTable samples, Attribute attribute, string value, out int positives, out int negatives)
            {
                positives = 0;
                negatives = 0;

                foreach (DataRow aRow in samples.Rows)
                {
                    if (((string)aRow[attribute.AttributeName] == value))
                        if ((bool)aRow[mTargetAttribute] == true)
                            positives++;
                        else
                            negatives++;
                }
            }

           
            private double gain(DataTable samples, Attribute attribute)
            {
                string[] values = attribute.values;
                double sum = 0.0;

                for (int i = 0; i < values.Length; i++)
                {
                    int positives, negatives;

                    positives = negatives = 0;

                    getValuesToAttribute(samples, attribute, values[i], out positives, out negatives);

                    double entropy = calcEntropy(positives, negatives);
                    sum += -(double)(positives + negatives) / mTotal * entropy;
                }
                return mEntropySet + sum;
            }


            private Attribute getBestAttribute(DataTable samples, Attribute[] attributes)
            {
                double maxGain = 0.0;
                Attribute result = attributes[0];
                //Attribute result = null;


                foreach (Attribute attribute in attributes)
                {
                    double aux = gain(samples, attribute);
                    if (aux >= maxGain)
                    {
                        maxGain = aux;
                        result = attribute;
                    }
                }
                return result;
            }
         
            private bool allSamplesPositives(DataTable samples, string targetAttribute)
            {
                foreach (DataRow row in samples.Rows)
                {
                    if ((bool)row[targetAttribute] == false)
                        return false;
                }

                return true;
            }

           
            private bool allSamplesNegatives(DataTable samples, string targetAttribute)
            {
                foreach (DataRow row in samples.Rows)
                {
                    if ((bool)row[targetAttribute] == true)
                        return false;
                }

                return true;
            }

          
            private ArrayList getDistinctValues(DataTable samples, string targetAttribute)
            {
                ArrayList distinctValues = new ArrayList(samples.Rows.Count);

                foreach (DataRow row in samples.Rows)
                {
                    if (distinctValues.IndexOf(row[targetAttribute]) == -1)
                        distinctValues.Add(row[targetAttribute]);
                }

                return distinctValues;
            }

      
            private object getMostCommonValue(DataTable samples, string targetAttribute)
            {
                ArrayList distinctValues = getDistinctValues(samples, targetAttribute);
                int[] count = new int[distinctValues.Count];

                foreach (DataRow row in samples.Rows)
                {
                    int index = distinctValues.IndexOf(row[targetAttribute]);
                    count[index]++;
                }

                int MaxIndex = 0;
                int MaxCount = 0;

                for (int i = 0; i < count.Length; i++)
                {
                    if (count[i] > MaxCount)
                    {
                        MaxCount = count[i];
                        MaxIndex = i;
                    }
                }

                return distinctValues[MaxIndex];
            }

          
            private TreeNode internalMountTree(DataTable samples, string targetAttribute, Attribute[] attributes)
            {
                if (allSamplesPositives(samples, targetAttribute) == true)
                    return new TreeNode(new Attribute(true));

                if (allSamplesNegatives(samples, targetAttribute) == true)
                    return new TreeNode(new Attribute(false));

                if (attributes.Length == 0)
                    return new TreeNode(new Attribute(getMostCommonValue(samples, targetAttribute)));

                mTotal = samples.Rows.Count;
                mTargetAttribute = targetAttribute;
                mTotalPositives = countTotalPositives(samples);

                mEntropySet = calcEntropy(mTotalPositives, mTotal - mTotalPositives);

                Attribute bestAttribute = getBestAttribute(samples, attributes);

                TreeNode root = new TreeNode(bestAttribute);

                DataTable aSample = samples.Clone();

                foreach (string value in bestAttribute.values)
                {
                   
                    aSample.Rows.Clear();

                    DataRow[] rows = samples.Select(bestAttribute.AttributeName + " = " + "'"  + value + "'");

                    foreach (DataRow row in rows)
                    {
                        aSample.Rows.Add(row.ItemArray);
                    }
                  
                    ArrayList aAttributes = new ArrayList(attributes.Length - 1);
                    for (int i = 0; i < attributes.Length; i++)
                    {
                        if (attributes[i].AttributeName != bestAttribute.AttributeName)
                            aAttributes.Add(attributes[i]);
                    }
          
                    if (aSample.Rows.Count == 0)
                    {
                        //return new TreeNode(new Attribute(getMostCommonValue(aSample, targetAttribute)));
                        return new TreeNode(new Attribute(null));
                    }
                    else
                    {
                        DecisionTreeID3 dc3 = new DecisionTreeID3();
                        TreeNode ChildNode =  dc3.mountTree(aSample, targetAttribute, (Attribute[])aAttributes.ToArray(typeof(Attribute)));
                        root.AddTreeNode(ChildNode, value);
                    }
                }

                return root;
            }



            public TreeNode mountTree(DataTable samples, string targetAttribute, Attribute[] attributes)
            {
                mSamples = samples;
                return internalMountTree(mSamples, targetAttribute, attributes);
            }
        }


        class ID3Sample
        {

            public static void printNode(TreeNode root, string tabs, StreamWriter writer)
            {
                
                writer.WriteLine(tabs + '|' + root.attribute + '|');

                Console.WriteLine(tabs + '|' + root.attribute + '|');

                if (root.attribute.values != null)
                {
                    
                    for (int i = 0; i < root.attribute.values.Length; i++)
                    {
                        Console.WriteLine(tabs + "\t" + "<" + root.attribute.values[i] + ">");
                        writer.WriteLine(tabs + "\t" + "<" + root.attribute.values[i] + ">");

                        TreeNode childNode = root.getChildByBranchName(root.attribute.values[i]);
                        printNode(childNode, "\t" + tabs, writer);
                    }               
                }
            }


            static DataTable getDataTable(string fileName, int count)
            {
                DataTable row = new DataTable("samples");






                /***
                Данные про шары
                   http://archive.ics.uci.edu/ml/datasets/Balloons
                ***/
                  
                DataColumn column = row.Columns.Add("color");
                column.DataType = typeof(string);

                column = row.Columns.Add("size");
                column.DataType = typeof(string);

                column = row.Columns.Add("act");
                column.DataType = typeof(string);

                column = row.Columns.Add("age");
                column.DataType = typeof(string);
                column = row.Columns.Add("result");
                column.DataType = typeof(bool);
                


                /***
             Данные про сердце
                http://archive.ics.uci.edu/ml/datasets/SPECT+Heart
             ***/
                         /*
                DataColumn column = row.Columns.Add("f1");
                column.DataType = typeof(string);

                column = row.Columns.Add("f2");
                column.DataType = typeof(string);

                column = row.Columns.Add("f3");
                column.DataType = typeof(string);

                column = row.Columns.Add("f4");
                column.DataType = typeof(string);

                column = row.Columns.Add("f5");
                column.DataType = typeof(string);

                column = row.Columns.Add("f6");
                column.DataType = typeof(string);

                column = row.Columns.Add("f7");
                column.DataType = typeof(string);

                column = row.Columns.Add("f8");
                column.DataType = typeof(string);

                column = row.Columns.Add("f9");
                column.DataType = typeof(string);

                column = row.Columns.Add("f10");
                column.DataType = typeof(string);

                column = row.Columns.Add("f11");
                column.DataType = typeof(string);

                column = row.Columns.Add("f12");
                column.DataType = typeof(string);

                column = row.Columns.Add("f13");
                column.DataType = typeof(string);

                column = row.Columns.Add("f14");
                column.DataType = typeof(string);

                column = row.Columns.Add("f15");
                column.DataType = typeof(string);

                column = row.Columns.Add("f16");
                column.DataType = typeof(string);

                column = row.Columns.Add("f17");
                column.DataType = typeof(string);

                column = row.Columns.Add("f18");
                column.DataType = typeof(string);

                column = row.Columns.Add("f19");
                column.DataType = typeof(string);

                column = row.Columns.Add("f20");
                column.DataType = typeof(string);

                column = row.Columns.Add("f21");
                column.DataType = typeof(string);

                column = row.Columns.Add("f22");
                column.DataType = typeof(string);

                column = row.Columns.Add("result");
                column.DataType = typeof(bool);

                                  */

                StreamReader reader = new StreamReader (fileName);
           
                while (!reader.EndOfStream)
                {
                    string curent = reader.ReadLine();
                    object[] elements = new object[count+1];

                    int startI=curent.IndexOf(',');
                    int len;
                    elements[0] = curent.Substring(0, startI);
                    for (int i=1; i<count; i++)
                    {
                        len = curent.IndexOf(',', startI + 1) - startI - 1;
                        elements[i] = curent.Substring(startI + 1, len);
                        startI = curent.IndexOf(',', startI + 1);    
                    }
                    if ((curent.Substring(startI+1, 1) == "t") || (curent.Substring(startI + 1, 1) == "1")|| (curent.Substring(startI + 1, 1) == "T"))
                        elements[count] = true; 
                    else
                        elements[count] = false;
                                       
                    row.Rows.Add(elements);

                }




                return row;
            }

      
            [STAThread]
            static void Main(string[] args)
            {



                /***
               Множество значений атрибутов про сердце                                    
               ***/
                /*
                   string fileName = "data_heart.txt";
                   int countAtr = 22;
                   Attribute f1 = new Attribute("f1", new string[] {"0", "1"});
                   Attribute f2 = new Attribute("f2", new string[] {"0", "1"});
                   Attribute f3 = new Attribute("f3", new string[] {"0", "1"});
                   Attribute f4 = new Attribute("f4", new string[] {"0", "1"});
                   Attribute f5 = new Attribute("f5", new string[] {"0", "1"});
                   Attribute f6 = new Attribute("f6", new string[] {"0", "1"});
                   Attribute f7 = new Attribute("f7", new string[] {"0", "1"});
                   Attribute f8 = new Attribute("f8", new string[] {"0", "1"});
                   Attribute f9 = new Attribute("f9", new string[] {"0", "1"});
                   Attribute f10 = new Attribute("f10", new string[] {"0", "1"});
                   Attribute f11= new Attribute("f11", new string[] {"0", "1"});
                   Attribute f12= new Attribute("f12", new string[] {"0", "1"});
                   Attribute f13= new Attribute("f13", new string[] {"0", "1"});
                   Attribute f14= new Attribute("f14", new string[] {"0", "1"});
                   Attribute f15= new Attribute("f15", new string[] {"0", "1"});
                   Attribute f16= new Attribute("f16", new string[] {"0", "1"});
                   Attribute f17= new Attribute("f17", new string[] {"0", "1"});
                   Attribute f18= new Attribute("f18", new string[] {"0", "1"});
                   Attribute f19= new Attribute("f19", new string[] {"0", "1"});
                   Attribute f20= new Attribute("f20", new string[] {"0", "1"});
                   Attribute f21= new Attribute("f21", new string[] {"0", "1"});  
                   Attribute f22= new Attribute("f22", new string[] {"0", "1"});
                   Attribute[] attributes = new Attribute[] { f1,f2,f3,f4,f5,f6,f7,f8,f9,f10,f11,f12,f13,f14,f15,f16,f17,f18,f19,f20,f21,f22};
               */

                /***
               Множество значений атрибутов про шары
               ***/

                string fileName = "data_balloons.txt";
                int countAtr = 4;
                Attribute color = new Attribute("color", new string[] {"YELLOW", "GREEN"});
                Attribute size = new Attribute("size", new string[] {"LARGE", "SMALL"});
                Attribute act = new Attribute("act", new string[] { "STRETCH", "DIP"});
                Attribute age = new Attribute("age", new string[] { "ADULT", "CHILD" });
                Attribute[] attributes = new Attribute[] { color, size, act, age};
                        





                DataTable samples = getDataTable(fileName, countAtr);

                DecisionTreeID3 id3 = new DecisionTreeID3();
                TreeNode root = id3.mountTree(samples, "result", attributes);

                StreamWriter writer = new StreamWriter ("out.txt");
                printNode(root, "", writer);
                writer.Close();


                Console.WriteLine("Дерево в файле out.txt. Нажмите Enter для завершения");
                Console.ReadLine();
            }
        }
    }

}

