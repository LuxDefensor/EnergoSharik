using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Energosphere
{
    public enum ParameterTypes
    {
        ActiveOut = 2,
        ActiveIn = 4,
        ReactiveOut = 6,
        ReactiveIn = 8,
        ActiveBalanceFact = 14,
        ActiveBalanceNormal = 68,
        ActiveBalancePercent = 69,
        ActiveOutRound = 152,
        ActiveInRound = 154,
        ReactiveOutRound = 156,
        ReactiveInRound = 158
    }

    public class Parameter
    {

        private ParameterTypes type;
        private int id;
        private AIIS aiis;

        public Parameter(AIIS aiis)
        {
            this.aiis = aiis;
        }

        public ParameterTypes Type
        {
            get
            {
                return type;
            }

            set
            {
                type = value;
            }
        }

        public string TypeName
        {
            get
            {
                switch (type)
                {
                    case ParameterTypes.ActiveIn:
                        return "А+";
                    case ParameterTypes.ActiveOut:
                        return "А-";
                    case ParameterTypes.ReactiveIn:
                        return "Р+";
                    case ParameterTypes.ReactiveOut:
                        return "Р-";
                    default:
                        return "параметр " + (int)type;
                }
            }
        }

        public int Id
        {
            get
            {
                return id;
            }

            set
            {
                id = value;
            }
        }

        /// <summary>
        /// Checks Ktr on the both ends of the time interval.
        /// If two Ktr values are the same then returns this value
        /// otherwise returns NULL
        /// </summary>
        /// <param name="id_pp"></param>
        /// <param name="dtStart"></param>
        /// <param name="dtEnd"></param>
        /// <returns></returns>
        public Nullable<double> GetKtr(DateTime dtStart, DateTime dtEnd)
        {
            double ktr1, ktr2;
            string sql = string.Format("select dbo.zzz_getcoef(dbo.pp_id_point({0}),{1}", id, dtStart);
            try
            {
                ktr1 = aiis.Model.GetDoubleValue(sql, 60);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка при получении значения коэффициента трансформации для {0} за {1}",
                    id, dtStart.ToString("yyyy-MM-dd"), ex));
            }
            sql = string.Format("select dbo.zzz_getcoef(dbo.pp_id_point({0}),{1}", id, dtEnd);
            try
            {
                ktr2 = aiis.Model.GetDoubleValue(sql, 60);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Ошибка при получении значения коэффициента трансформации для {0} за {1}",
                    id, dtEnd.ToString("yyyy-MM-dd"), ex));
            }
            if (ktr1 == ktr2)
                return ktr1;
            else
                return null;
        }

        public Point ParentPoint
        {
        get
            {
                return aiis.AllPoints.First(p => p.Parameters.Contains(this));
            }
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", ParentPoint.Name, this.TypeName);
        }

    }
}
