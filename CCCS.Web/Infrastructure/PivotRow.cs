using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCCS.Infrastructure
{
    /// <summary>
    /// Store the row of the Pivot result.
    /// </summary>
    /// <typeparam name="TypeId">Type of ObjectId</typeparam>
    /// <typeparam name="TypeAttr">Type of Attribute</typeparam>
    /// <typeparam name="TypeValue">Type of Value</typeparam>
    public class PivotRow<TypeId, TypeAttr, TypeValue>
    {
        public TypeId ObjectId { get; set; }
        public IEnumerable<TypeAttr> Attributes { get; set; }
        public IEnumerable<TypeValue> Values { get; set; }

        /// <summary>
        /// Get the Pivot table
        /// </summary>
        /// <param name="attributeNames">the list of attributes</param>
        /// <param name="source">the data of Pivot source</param>
        /// <returns>the Pivot table</returns>
        public static DataTable GetPivotTable(List<TypeAttr> attributeNames,
            List<PivotRow<TypeId, TypeAttr, TypeValue>> source)
        {
            DataTable dt = new DataTable();

            DataColumn dc = new DataColumn("ID", typeof(TypeId));
            dt.Columns.Add(dc);

            // Creat the new DataColumn for each attribute
            attributeNames.ForEach(name =>
            {
                dc = new DataColumn(name.ToString(), typeof(TypeValue));
                dt.Columns.Add(dc);
            });

            // Insert the data into the Pivot table
            foreach (PivotRow<TypeId, TypeAttr, TypeValue> row in source)
            {
                DataRow dr = dt.NewRow();
                dr["ID"] = row.ObjectId;

                List<TypeAttr> attributes = row.Attributes.ToList();
                List<TypeValue> values = row.Values.ToList();

                // Set the value basing the attribute names.
                for (int i = 0; i < values.Count; i++)
                {
                    var currentValue = (attributes[i] == null)? "?": dr[attributes[i].ToString()].ToString();
                    if (!String.IsNullOrEmpty(currentValue))
                    {
                        dr[attributes[i].ToString()] = decimal.Parse(currentValue) + decimal.Parse(values[i].ToString());
                    }
                    else
                    {
                        dr[attributes[i].ToString()] = values[i].ToString();
                    }
                }

                dt.Rows.Add(dr);
            }

            return dt;
        }
    }
}

