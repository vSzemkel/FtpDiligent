create or replace PROCEDURE "MODIFY_SCHEDULE2" (
  vmode in binary_integer,
  vxx in out ftp_schedule.xx%type,
  vendxx in ftp_schedule.end_xx%type,
  vnazwa in varchar2,
  vjobstart in ftp_schedule.job_start%type,
  vjobstop in ftp_schedule.job_stop%type,
  vjobstep in ftp_schedule.job_step%type,
  vdisabled in ftp_schedule.disabled%type
) as
  vmsg varchar2(512);
begin
  if vjobstop<vjobstart then
     raise_application_error(-20001,'Data zakonczenia musi byc pozniejsza niz data rozpoczecia');
  end if;
  
  if vmode=0 then --INSERT
     insert into ftp_schedule (end_xx,nazwa,job_start,job_stop,job_step,disabled)
     values (vendxx,vnazwa,vjobstart,vjobstop,vjobstep,decode(vdisabled,0,null,1))
     returning xx into vxx;
  elsif vmode=1 then --UPDATE    
    update ftp_schedule fs set 
      fs.nazwa=vnazwa,
      fs.job_start=vjobstart,
      fs.job_stop=vjobstop,
      fs.job_step=vjobstep,
      fs.disabled=decode(vdisabled,0,null,1)
     where fs.xx=vxx;
  else --vmode=2 DELETE
     update ftp_schedule fs set fs.usuniety = 1
      where fs.xx=vxx;
  end if;
end modify_schedule2;