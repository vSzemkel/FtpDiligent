create or replace function check_file (
   vins_xx in number,
   vfile_name varchar2,
   vfile_size number,
   vfile_date date
) return binary_integer is
   vcc binary_integer;
begin --add_months() by obejsc blad odczytu filesystemu ftp.agora.pl
   select count(1) into vcc
     from ftp_endpoint e,ftp_schedule s,ftp_file f
    where e.ins_xx=vins_xx and e.xx=s.end_xx(+)
      and ((s.xx=f.sch_xx and f.manual_sync is null) or (e.xx=f.end_xx and f.manual_sync is not null))
      and f.file_date in (vfile_date,add_months(vfile_date,-12),add_months(vfile_date,12))
      and f.file_name=vfile_name and f.file_size=vfile_size;

   return vcc;
end check_file;