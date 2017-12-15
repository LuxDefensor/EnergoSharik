using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Energosphere
{

    public class AIIS
    {
        private string name;
        private string description;
        private List<Point> roots;
        private List<Point> allPoints;
        private List<Parameter> allParameters;
        private DataModel m;

        public delegate void OnPointsUpdate(PointEventArgs e);
        public event OnPointsUpdate PointsUpdate;

        public string Name
        {
            get
            {
                return name;
            }

            set
            {
                name = value;
            }
        }

        public DataModel Model
        {
        get
            {
                return m;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }

            set
            {
                description = value;
            }
        }

        public List<Point> Roots
        {
            get
            {
                return roots;
            }
        }

        public List<Point> AllPoints
        {
            get
            {
                return allPoints;
            }
        }

        public List<Parameter> AllParameters
        {
        get
            {
                return allParameters;
            }
        }

        public AIIS()
            : this(rootIDs: null)
        {
        }

        public AIIS(int[] rootIDs)
        {
            allPoints = new List<Point>();
            allParameters = new List<Parameter>();
            m = new DataModel();
            roots = GetRoots(rootIDs);
        }

        public AIIS(string settingsFile) : this(settingsFile, null)
        {

        }

        public AIIS(string settingsFile, int[] rootIDs) 
        {
            allPoints = new List<Point>();
            allParameters = new List<Parameter>();
            m = new DataModel(settingsFile);
            roots = GetRoots(rootIDs);
        }

        public List<Point> Search(string searchString)
        {
            List<Point> children;
            List<Point> result = new List<Point>();
            Point current;
            string sql = string.Format(@"
select id_point from points where pointname like '%{0}%'
and dbo.treerootid(id_point) in ({1})",
                searchString, string.Join(",", roots.Select(r => r.ID.ToString())));
            DataTable found = m.GetData(sql, 60);
            foreach (DataRow row in found.Rows)
            {
                current = this.GetPoint((int)row[0]);
                if (current == null)
                {
                    int[] path = GetPathReversed((int)row[0]);
                    foreach (int id in path)
                    {
                        current = this.GetPoint(id);
                        if (current == null)
                            break;
                        if (!current.Loaded)
                        {
                            children = current.Children;
                            PointsUpdate?.Invoke(new PointEventArgs() { Points = children, ParentPoint = current });
                        }
                    }
                }
                if (current?.ID == (int)row[0])
                    result.Add(current);
            }
            return result;
        }

        private List<Point> GetRoots(int[] ids = null)
        {
            List<Point> result = new List<Point>();
            string sql;
            if (ids == null)
                sql = "select id_point, pointname, point_type from points where id_parent is null and point_type in (1,39)";
            else
                sql = string.Format("select id_point, pointname, point_type from points where id_point in ({0})",
                    string.Join(",", ids));
            DataTable points = m.GetData(sql, 60);
            foreach (DataRow row in points.Rows)
            {
                result.Add(new Point((int)row[0], null, row[1].ToString(), (PointTypes)row[2], this));
            }
            allPoints.AddRange(result);
            return result;
        }

        public Point GetPoint(int idPoint)
        {
            return allPoints.FirstOrDefault(p => p.ID == idPoint);
        }

        public Point LoadPoint(int idPoint)
        {
            List<Point> children;
            Point current = null;
            int[] path = GetPathReversed(idPoint);
            if (path.Length == 0)
                return null;
            foreach (int p in path)
            {
                current = GetPoint(p);
                if (current == null)
                    return null;
                else
                {
                    if (!current.Loaded)
                    {
                        children = current.Children;
                        PointsUpdate?.Invoke(new PointEventArgs() { ParentPoint = current, Points = children });
                    }
                }
            }
            return current;
        }

        public void LoadAllPoints()
        {
            foreach (Point p in roots)
            {
                LoadChildren(p);
            }
        }

        public void LoadSubtree(Point subroot)
        {
            foreach (Point child in subroot.Children)
            {
                LoadChildren(child);
            }
        }

        private void LoadChildren(Point point)
        {
            List<Point> children;
            children = point.Children;
            PointsUpdate?.Invoke(new PointEventArgs() { ParentPoint=point,Points=children});
            foreach (Point c in children)
            {
                LoadChildren(c);
            }
        }

        public Parameter LoadParameter(string id_pp)
        {
            string sql = "select id_point from pointparams where id_pp=" + id_pp;
            int idPoint = m.GetIntValue(sql, 30);
            Point parent = LoadPoint(idPoint);
            return parent.Parameters.First(p => p.Id.ToString() == id_pp);
        }

        private int[] GetPathReversed(int idPoint)
        {
            string sql = string.Format(@"
declare @look_id int
set @look_id={0};

with tree(id_point,id_parent,lvl)
 as(
select p.ID_Point,p.ID_Parent, 1 lvl
from points p
where p.ID_Point=@look_id
union all
select p2.ID_Point,p2.ID_Parent, tree.lvl+1
from tree inner join points p2 on tree.ID_Parent=p2.ID_Point)

select id_point,lvl from tree
order by lvl desc", idPoint);
            DataTable found = m.GetData(sql, 60);
            if (found.Rows.Count == 0)
                return null;
            else
            {
                int[] result = new int[found.Rows.Count];
                for (int i = 0; i < found.Rows.Count; i++)
                {
                    result[i] = (int)found.Rows[i][0];
                }
                return result;
            }
        }

    }
}
