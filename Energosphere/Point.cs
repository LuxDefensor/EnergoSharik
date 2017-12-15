using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Energosphere
{
    public enum PointTypes
    {
        GenericObject = 1,
        Department = 2,
        Substation = 5,
        RU = 7,
        BusSection = 8,
        BypassSwitch = 9,
        Feeder = 10,
        ParametersGroup = 11,
        FeederWithBypass = 12,
        PointParameter = 17,
        USPD = 19,
        Channel = 20,
        Meter = 21,
        Abstaract = 29,
        Neighbor = 39,
        Scanner = 47,
        Equipment = 56,
        TT = 81,
        TN = 85,
        Building = 144,
        Room = 145,
        RES = 147,
        PES = 148,
        SupplyPoint = 149,
        LineLink = 239,
        PointLink = 255
    }
    public class Point
    {

        private PointTypes type;
        private string name;
        private int id;
        private int? parentID;
        private List<Parameter> parameters;
        private List<Point> children;
        private bool loaded;
        private bool leaf;
        private AIIS aiis;

        public Point(int idPoint, int? idParent, string pointName, PointTypes pointType, AIIS aiis)
        {
            children = new List<Point>();
            parameters = new List<Parameter>();
            type = pointType;
            name = pointName;
            id = idPoint;
            parentID = idParent;
            loaded = false;
            leaf = false;
            this.aiis = aiis;
            LoadParameters();
        }

        private void LoadChildren()
        {
            if (!loaded)
            {
                string sql = string.Format(@"
select id_point, pointname, point_type from points
where id_parent={0}", id);
                DataTable childrenPoints = aiis.Model.GetData(sql, 120);
                if (childrenPoints.Rows.Count == 0)
                    leaf = true;
                else
                    foreach (DataRow row in childrenPoints.Rows)
                    {
                        children.Add(new Point((int)row[0], id, row[1].ToString(), (PointTypes)row[2], aiis));
                    }
                aiis.AllPoints.AddRange(children);
                loaded = true;
            }

        }

        public Point GetDescendant(int idPoint)
        {
            if (this.id == idPoint)
                return this;
            if (loaded)
                foreach (Point p in children)
                {
                    if (p.id == idPoint)
                        return p;
                    return p.GetDescendant(idPoint);
                }
            return null;
        }

        private void LoadParameters()
        {
            string sql = string.Format("select id_pp, id_param FROM PointParams WHERE id_point={0} and id_param in (2,4,6,8)", id);
            DataTable pointParams = aiis.Model.GetData(sql, 30);
            foreach (DataRow row in pointParams.Rows)
            {
                parameters.Add(new Parameter(aiis) { Id = (int)row[0], Type = (ParameterTypes)row[1] });
            }
            if (parameters.Count > 0)
                aiis.AllParameters.AddRange(parameters);
        }

        public int ID
        {
            get
            {
                return id;
            }
        }

        public int? ParentID
        {
        get
            {
                return parentID;
            }
        }

        public List<Point> Children
        {
            get
            {
                if (!loaded)
                    LoadChildren();
                return children;
            }
        }

        public PointTypes Type
        {
            get
            {
                return type;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public Point GetAncestor(PointTypes ancestorType)
        {
            /* Database version 
            string sql = string.Format(@"
declare @look_id int
set @look_id={0};

with tree(id_point, id_parent,pointname,point_type,lvl)
 as(
select p.ID_Point,p.ID_Parent,p.PointName,p.Point_Type, 1 lvl
from points p
where p.ID_Point=@look_id
union all
select p2.ID_Point,p2.ID_Parent,p2.PointName,p2.Point_Type, tree.lvl+1
from tree inner join points p2 on tree.ID_Parent=p2.ID_Point)

select top id_point from tree 
where point_type={1}
order by lvl desc", id, (int)type);
            int result = DataModel.GetIntValue(sql, 30);
            */
            Point current = this;
            PointTypes criterion = type;
            bool found = false;
            while (!aiis.Roots.Contains(current))
            {
                criterion = current.ParentPoint.Type;
                if (criterion == ancestorType)
                {
                    found = true;
                    break;
                }
                else
                    current = current.ParentPoint;
            }
            if (found)
                return current.ParentPoint;
            else
                return null;
        }

        public Point ParentPoint
        {
        get
            {
                return aiis.AllPoints.First(p => p.ID == parentID);
            }
        }

        public List<Parameter> Parameters
        {
            get
            {
                return parameters;
            }
        }

        public bool Loaded
        {
        get
            {
                return loaded;
            }
        }

        public string SubstationName()
        {
            string sql = string.Format("select dbo.zzz_getps({0})", id);
            string result = aiis.Model.GetStringValue(sql, 30);
            return result;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
