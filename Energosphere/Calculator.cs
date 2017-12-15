using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace Energosphere
{
    public class Calculator
    {
        private DataModel m;

        public Calculator(string settingsFile)
        {
            m = new DataModel(settingsFile);
        }

        public Calculator()
        {
            m = new DataModel();
        }
        public DataTable MeterLogs(List<Parameter> pps, DateTime dtStart, DateTime dtEnd)
        {
            string sql = string.Format(@"
select dbo.zzz_getps(pp.id_point) Подстанция,
(select pointname from points where id_point=pp.ID_Point) Счетчик,
d.dt Дата, d.description Описание,d.comment Дополнительно
from pointparams pp
inner join schemacontents sc_high on pp.ID_PP=sc_high.ID_PP and
sc_high.RefIsPoint=2
inner join schemacontents sc_low on sc_high.ID_Ref=sc_low.ID_PP and
sc_low.refispoint=1
inner join channels_main c1 on c1.ID_Channel=sc_low.ID_Ref and
sc_low.RefIsPoint=1
inner join channels_main c2 on c2.ID_USPD=c1.ID_USPD and c2.TypeChan='J'
inner join vwDiscretsWithDesc d on d.id_channel=c2.ID_Channel
where d.dt between '{0}' and '{1}'
and sc_high.id_pp in ({2})
order by 1,2,3 desc",
                    dtStart.ToString("yyyyMMdd"), dtEnd.ToString("yyyyMMdd"),
                    string.Join(",", pps.Select(p => p.Id)));
            return m.GetData(sql, 120);
        }

        public DataTable FixedValues(string id_pp, DateTime dtStart, DateTime dtEnd, bool withKtr, bool measuredOnly)
        {
            StringBuilder sql = new StringBuilder();
            if (measuredOnly)
            {
                sql.AppendFormat(@"declare @dates table (dt0 datetime)
declare @dt1 datetime, @dt2 datetime, @dtcurrent datetime
set @dt1='{0}'
set @dt2='{1}'
set @dtcurrent=@dt1
while @dtcurrent<=@dt2
begin
	insert into @dates(dt0) values(@dtcurrent)
	set @dtcurrent=DATEADD(day,1,@dtcurrent)
end

select n.DT,n.Val, n.State
from @dates d
outer apply 
(select ni.DT,ni.Val,ni.State from PointNIs_On_Main_Stack ni 
 inner join SchemaContents sc on sc.ID_Ref=ni.ID_PP and sc.RefIsPoint=2
where sc.ID_PP={2} and d.dt0=ni.dt and ni.DT between sc.DT1 and sc.DT2) as n
", dtStart.ToString("yyyyMMdd"), dtEnd.ToString("yyyyMMdd"), id_pp);
            }
            else
            {
                if (withKtr)
                {
                    sql.AppendFormat("select dt, value, state from dbo.f_Get_PointNIs({0},'{1}','{2}',3,0,1,null,0,null,null,null)",
                                     id_pp, dtStart.ToString("yyyyMMdd"), dtEnd.ToString("yyyyMMdd"));
                }
                else
                {
                    sql.Append("select n.dt, n.value, n.state from schemacontents sc cross apply ");
                    sql.AppendFormat("dbo.f_Get_PointNIs(id_ref,'{0}','{1}',3,0,1,null,0,null,null,null) n ",
                                     dtStart.ToString("yyyyMMdd"), dtEnd.ToString("yyyyMMdd"));
                    sql.AppendFormat("where id_pp={0} and n.DT between sc.DT1 and sc.DT2", id_pp);
                }
            }
            return m.GetData(sql.ToString(), 120);
        }

        public DataTable PairOfFixedValues(string id_pp, DateTime dtStart, DateTime dtEnd)
        {
            DataTable result;
            string sql = string.Format(@"
select dt,value,state 
from dbo.f_Get_PointNIs({0},'{1}','{2}',0,default,default,default,default,default,default,default) n1",
                id_pp, dtStart.ToString("yyyyMMdd"), dtEnd.ToString("yyyyMMdd"));
            result = m.GetData(sql, 120);
            if (result.Rows.Count != 2)
            {
                result.Rows.Clear();
                result.Rows.Add(dtStart, null, 1);
                result.Rows.Add(dtEnd, null, 1);
            }
            return result;
        }

        public DataTable DailyValues(string id_pp, DateTime dtStart, DateTime dtEnd)
        {
            DataTable result = new DataTable();
            string sql = string.Format(
                     "select dt,value,state from dbo.f_Get_PointProfile({0},'{1}','{2}',3,null,null,null,null,null)",
                     id_pp, dtStart.ToString("yyyyMMdd"), dtEnd.AddDays(1).ToString("yyyyMMdd"));
            result = m.GetData(sql, 180);

            return result;
        }

        public DataTable HourValues(string id_pp, DateTime dtStart, DateTime dtEnd)
        {
            DataTable result = new DataTable();
            string sql = string.Format(
                                "select dt,value,state from dbo.f_Get_PointProfile({0},'{1}','{2}',2,null,null,null,null,null)",
                                id_pp, dtStart.ToString("yyyyMMdd"), dtEnd.AddDays(1).ToString("yyyyMMdd"));
            result = m.GetData(sql, 300);
            return result;
        }

        public  DataTable HalfhourValues(string id_pp, DateTime dtStart, DateTime dtEnd)
        {
            DataTable result = new DataTable();
            string sql = string.Format(
                "select dt,value,state from dbo.f_Get_PointProfile({0},'{1}','{2}',1,null,null,null,null,null)",
                id_pp, dtStart.ToString("yyyyMMdd"), dtEnd.AddDays(1).ToString("yyyyMMdd"));
            result = m.GetData(sql, 300);
            return result;
        }

        public  DataTable GetPercentMains(List<Parameter> pps, DateTime dtStart, DateTime dtEnd)
        {
            DataTable result = new DataTable();
            string sql = string.Format(@"
declare @dt1 datetime, @dt2 datetime
declare @halfhours int

set @dt1='{0}'
set @dt2=DATEADD(minute,-30,'{1}')
set @halfhours=DATEDIFF(HOUR,@dt1,@dt2)*2

if @halfhours=0 set @halfhours=1;

with src as(
select p.id_point, dbo.zzz_GetPS(p.ID_Point) PS, PointName, ni.DT, ni.ID_PP meter, pp.ID_PP feeder
from points p left join pointparams pp on p.ID_Point=pp.ID_Point
left join SchemaContents sc on pp.ID_PP=sc.ID_PP and sc.RefIsPoint=2
left join PointMains ni on ni.ID_PP=sc.ID_Ref
where pp.ID_PP in ({2})
and ni.dt between @dt1 and @dt2 and sc.DT1<@dt1 and sc.DT2>@dt2)

select dbo.zzz_GetPS(p.ID_Point) PS, PointName,
100 * (select count(*) from src where src.ID_Point=p.ID_Point)/@halfhours/count(id_pp) PC,
(select max(dt) from pointmains m
 right join pointparams pp1 on pp1.ID_PP=m.ID_PP
 where pp1.ID_Point=p.ID_point) LastDate
from points p left join pointparams pp on p.ID_Point=pp.ID_Point
where pp.ID_PP in ({2})
group by p.ID_Point,PointName",
                dtStart.ToString("yyyyMMdd"), dtEnd.ToString("yyyyMMdd"),
                string.Join(",", pps.Select(p => p.Id)));
            result = m.GetData(sql, 300);
            if (result.Rows.Count == 0)
            {
                throw new Exception("The query returned empty rowset in Calculator.GetPercentMains");
            }
            return result;
        }

        public DataTable GetPercentNIs(List<Parameter> pps, DateTime dtStart, DateTime dtEnd)
        {
            DataTable result = new DataTable();
            string sql = string.Format(@"
declare @dt1 datetime, @dt2 datetime
declare @days int

set @dt1='{0}'
set @dt2='{1}'
set @days=DATEDIFF(DAY,@dt1,@dt2)

if @days=0 set @days=1;

with src as(
select p.id_point, dbo.zzz_GetPS(p.ID_Point) PS, PointName, ni.DT, ni.ID_PP meter, pp.ID_PP feeder
from points p left join pointparams pp on p.ID_Point=pp.ID_Point
left join SchemaContents sc on pp.ID_PP=sc.ID_PP and sc.RefIsPoint=2
left join PointNIs_On_Main_Stack ni on ni.ID_PP=sc.ID_Ref
where pp.ID_PP in ({2})
and ni.dt between @dt1 and @dt2 and sc.DT1<@dt1 and sc.DT2>@dt2)

select dbo.zzz_GetPS(p.ID_Point) PS, PointName,
100 * (select count(*) from src where src.ID_Point=p.ID_Point)/@days/count(id_pp) PC,
(select max(dt) from src
 right join pointparams pp1 on pp1.id_pp=src.feeder
 where pp1.ID_Point=p.ID_Point) LastDate
from points p left join pointparams pp on p.ID_Point=pp.ID_Point
where pp.ID_PP in ({2})
group by p.ID_Point,PointName",
                dtStart.ToString("yyyyMMdd"), dtEnd.ToString("yyyyMMdd"),
                string.Join(",", pps.Select(p => p.Id)));
            result = m.GetData(sql, 300);
            if (result.Rows.Count == 0)
            {
                throw new Exception("The query returned empty rowset in Calculator.GetPercentNIs");
            }
            return result;
        }

        public DataTable GetPercentLogs(List<Parameter> pps, DateTime dtStart, DateTime dtEnd)
        {
            DataTable result = new DataTable();
            string sql = string.Format(@"
declare @dt1 datetime, @dt2 datetime

set @dt1='{0}'
set @dt2='{1}';

select dbo.zzz_getps(pp.id_point) ps,
(select pointname from points where id_point=pp.ID_Point) feeder,
case count(*)
when 0 then 0
else 100
end pc,
max(d.dt)
from pointparams pp
inner join schemacontents sc_high on pp.ID_PP=sc_high.ID_PP and
sc_high.RefIsPoint=2
inner join schemacontents sc_low on sc_high.ID_Ref=sc_low.ID_PP and
sc_low.refispoint=1
inner join channels_main c1 on c1.ID_Channel=sc_low.ID_Ref and
sc_low.RefIsPoint=1
inner join channels_main c2 on c2.ID_USPD=c1.ID_USPD and c2.TypeChan='J'
inner join vwDiscretsWithDesc d on d.id_channel=c2.ID_Channel
where d.dt between @dt1 and @dt2
and sc_high.id_pp in ({2})
group by pp.id_point
order by 1,2",
                 dtStart.ToString("yyyyMMdd"), dtEnd.ToString("yyyyMMdd"),
                 string.Join(",", pps.Select(p => p.Id)));
            result = m.GetData(sql, 300);
            if (result.Rows.Count == 0)
            {
                result.Rows.Add("Журнал пуст", "", 0, null);
            }
            return result;
        }
    }
}
