CREATE OR REPLACE FORCE EDITIONABLE VIEW "FTPGETWORK"."INSTANCE_SCHEDULE" ("INS_XX", "END_XX", "SCH_XX", "NAZWA", "START_DATE", "END_DATE", "START_JOB", "STOP_JOB", "STEP", "ILE_RAZY", "ENABLED") AS 
select i.xx,e.xx,s.xx,s.nazwa,get_week_start+(s.job_start/1440),get_week_start+(s.job_stop/1440),
to_char(get_week_start+(s.job_start/1440),'fmDay')||' '||to_char(get_week_start+(s.job_start/1440),'hh24:mi'),
to_char(get_week_start+(s.job_stop/1440),'fmDay')||' '||to_char(get_week_start+(s.job_stop/1440),'hh24:mi'),
s.job_step,1+trunc((s.job_stop-s.job_start)/s.job_step) ile_razy,decode(s.disabled,null,'TAK','NIE')
from ftp_worker_instance i,ftp_endpoint e,ftp_schedule s
where i.xx=e.ins_xx and e.xx=s.end_xx and e.usuniety is null and s.usuniety is null
order by 4;
