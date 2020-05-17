create or replace PROCEDURE "MODIFY_ENDPOINT" (
  vmode in binary_integer,
  vxx in out ftp_endpoint.xx%type,
  vinsxx in ftp_endpoint.ins_xx%type,
  vhost in ftp_endpoint.host%type,
  vuserid in ftp_endpoint.userid%type,
  vpasswd in ftp_endpoint.passwd%type,
  vremdir in ftp_endpoint.remote_dir%type,
  vlocdir in ftp_endpoint.local_dir%type,
  vprot in ftp_endpoint.protocol%type default 1,
  vdir in ftp_endpoint.direction%type default 1,
  vtransfer_mode in ftp_endpoint.transfer_mode%type default 2
) as
  vmsg varchar2(512);
begin
  if vmode=0 then --INSERT
     insert into ftp_endpoint (ins_xx,host,userid,passwd,remote_dir,local_dir,protocol,direction,transfer_mode)
     values (vinsxx,vhost,vuserid,vpasswd,vremdir,vlocdir,vprot,vdir,vtransfer_mode)
     returning xx into vxx;
  elsif vmode=1 then --UPDATE
    update ftp_endpoint fe
    set fe.host=vhost,fe.userid=vuserid,fe.passwd=vpasswd,fe.remote_dir=vremdir,
        fe.local_dir=vlocdir,fe.protocol=vprot,fe.direction=vdir,fe.transfer_mode=vtransfer_mode
    where fe.xx=vxx;
  else --vmode=2 DELETE
     delete from ftp_schedule where end_xx=vxx and usuniety=1;
     delete from ftp_endpoint fe
     where fe.xx=vxx;
  end if;
end modify_endpoint;