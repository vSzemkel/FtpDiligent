
/* create login [ftp] with password = 'siusiak'           */
/* create user [ftp] with default_schema=[ftp]             */
/* exec sp_addrolemember 'db_owner', 'ftp'                 */
/* create schema ftp with authorization ftp                */


drop table if exists [ftp_file]
go
drop table if exists [ftp_schedule]
go
drop table if exists [ftp_endpoint]
go
drop table if exists [ftp_instance]
go

create table [ftp_instance] (
    xx int identity not null,
    hostname varchar(128) not null,
    disposition varchar(128) default 'do ustalenia' not null,
    administrator varchar(64) default 'do ustalenia' not null,
    email2adm varchar(64) default 'do ustalenia' not null,
    created datetime2 constraint df_instance_creation default getdate() not null,
    disabled bit null,
    constraint pk_instance primary key clustered (xx),
)
go

create table [ftp_endpoint] (
    xx int identity not null,
    ins_xx int not null,
    host varchar(64) not null,
    userid varchar(32) not null,
    passwd varchar(32) not null,
    local_dir varchar(256) not null,
    remote_dir varchar(256) not null,
    refresh_date datetime2 constraint df_endpoint_refresh default getdate() not null,
    direction tinyint not null,
    transfer_mode tinyint not null,
    disabled bit,
    constraint pk_endpoint primary key clustered (xx),
    constraint fk_endpoint_instance foreign key (ins_xx) references [ftp_instance] (xx)
)
go

create table [ftp_schedule] (
    xx int identity not null,
    end_xx int not null,
    name varchar(128) not null,
    job_start smallint not null,
    job_stop smallint not null,
    job_stride smallint not null,
    deleted bit,
    disabled bit,
    constraint pk_schedule primary key clustered (xx),
    constraint fk_schedule_endpoint foreign key (end_xx) references [ftp_endpoint] (xx)
)
go

create table [ftp_file] (
    xx int identity not null,
    end_xx int null,
    sch_xx int null,
    file_name varchar(128) not null,
    file_size bigint not null,
    file_date datetime2 not null,
    completion_date datetime2 constraint df_file_completion default getdate(),
    direction tinyint not null,
    transfer_mode tinyint not null,
    manual_sync bit,
    disabled bit,
    constraint pk_file primary key clustered (xx),
    constraint fk_file_endpoint foreign key (end_xx) references [ftp_endpoint] (xx),
    constraint fk_file_schedule foreign key (sch_xx) references [ftp_schedule] (xx)
)
go



drop procedure sp_init_instance
go
create procedure sp_init_instance (
    @hostname varchar(128)
) as begin
    declare @xx int
    select @xx=xx from ftp_instance where hostname=@hostname
    if @xx is null
        insert into ftp_instance (hostname) output inserted.xx values (@hostname)
    else
        select @xx
end
go

drop procedure sp_modify_endpoint
go
create procedure sp_modify_endpoint (
    @mode tinyint,
    @xx int,
    @ins_xx int,
    @host varchar(64),
    @userid varchar(32),
    @passwd varchar(32),
    @remdir varchar(256),
    @locdir varchar(256),
    @direction tinyint,
    @transfer_mode tinyint
) as begin
  if @mode=0
     insert into ftp_endpoint (ins_xx,host,userid,passwd,remote_dir,local_dir,direction,transfer_mode)
     output inserted.xx
     values (@ins_xx,@host,@userid,@passwd,@remdir,@locdir,@direction,@transfer_mode)
  else if @mode=1
    update ftp_endpoint
    set host=@host,userid=@userid,passwd=@passwd,remote_dir=@remdir,
        local_dir=@locdir,direction=direction,transfer_mode=@transfer_mode
    where xx=@xx
  else begin
     delete from ftp_schedule where end_xx=@xx and deleted=1;
     delete from ftp_endpoint where xx=@xx;
  end
end
go

drop procedure sp_modify_schedule
go
create procedure sp_modify_schedule (
    @mode tinyint,
    @xx int,
    @end_xx int,
    @name varchar(128),
    @job_start smallint,
    @job_stop smallint,
    @job_stride smallint,
    @disabled bit
) as begin
  if @job_stop < @job_start
    raiserror(N'Data zakonczenia musi byc pozniejsza niz data rozpoczecia', 16, 1);
  
  if @mode=0
     insert into ftp_schedule (end_xx,name,job_start,job_stop,job_stride,disabled)
     output inserted.xx
     values (@end_xx,@name,@job_start,@job_stop,@job_stride,case @disabled when 0 then null else 1 end)
  else if @mode=1
    update ftp_schedule set name=@name,disabled=case @disabled when 0 then null else 1 end,
           job_start=@job_start,job_stop=@job_stop,job_stride=@job_stride
     where xx=@xx;
  else
     update ftp_schedule set deleted=1 where xx=@xx;
end
go

drop procedure sp_check_file
go
create procedure sp_check_file (
    @ins_xx int,
    @file_name varchar(128),
    @file_size bigint,
    @file_date datetime2
) as begin
    declare @cnt int
    select @cnt=count(1) from ftp_endpoint e left outer join ftp_schedule s on e.xx=s.end_xx,ftp_file f
     where e.ins_xx=@ins_xx
       and ((s.xx=f.sch_xx and f.manual_sync is null) or (e.xx=f.end_xx and f.manual_sync is not null))
       and f.file_date=@file_date and f.file_name=@file_name and f.file_size=@file_size
    if @cnt > 0
        select 1
    select 0
end
go

drop procedure sp_endpoint_for_schedule
go
create procedure sp_endpoint_for_schedule (
    @xx int
) as begin
    if @xx > 0 begin
        select fe.host,fe.userid,fe.passwd,fe.remote_dir,fe.local_dir,fe.refresh_date at time zone 'Central European Standard Time',fe.direction,fe.transfer_mode
          from ftp_endpoint fe,ftp_schedule fs
         where fs.xx=@xx and fs.end_xx=fe.xx
    end else
        select host,userid,passwd,remote_dir,local_dir,refresh_date at time zone 'Central European Standard Time',direction,transfer_mode
          from ftp_endpoint where xx=-@xx
end
go

drop procedure sp_select_next_sync
go
create procedure sp_select_next_sync (
    @ins_xx int
) as begin
    select * from (select top 1 fs.xx,name,ftp.sf_get_next_sync(fs.xx) as sync
      from ftp_endpoint fe,ftp_schedule fs
     where fe.ins_xx=@ins_xx and fe.xx=fs.end_xx
       and fe.disabled is null and fs.disabled is null
       and ftp.sf_get_week_minute() < fs.job_stop
     order by sync,refresh_date,job_stride) t
 union all
    select -1,'next week',dateadd(day,7,ftp.sf_get_week_start()) as sync
      from ftp_endpoint fe,ftp_schedule fs 
     where fe.ins_xx=@ins_xx and fe.xx=fs.end_xx
end
go

drop procedure sp_log_activation
go
create procedure sp_log_activation (
    @xx int,
    @refresh_date datetime2
) as begin
   if @xx > 0
      update ftp_endpoint set refresh_date=@refresh_date
       where xx in (select end_xx from ftp_schedule where xx=@xx)
   else
      update ftp_endpoint set refresh_date=@refresh_date where xx=-@xx
end
go

drop procedure sp_log_download
go
create procedure sp_log_download (
    @direction tinyint,
    @xx int,
    @refresh_date datetime2,
    @file_name varchar(128),
    @file_size bigint,
    @file_date datetime2
) as begin
    if @xx > 0 begin
        update ftp_endpoint set refresh_date=@refresh_date
         where xx in (select end_xx from ftp_schedule where xx=@xx)
        insert into ftp_file (sch_xx,file_name,file_size,file_date,manual_sync,direction,transfer_mode)
             select @xx,@file_name,@file_size,@file_date,null,@direction,transfer_mode
               from ftp_endpoint fe,ftp_schedule fs
              where fe.xx=fs.end_xx and fs.xx=@xx
    end else begin
        update ftp_endpoint set refresh_date=@refresh_date where xx=-@xx
        insert into ftp_file (end_xx,file_name,file_size,file_date,manual_sync,direction,transfer_mode)
             select -@xx,@file_name,@file_size,@file_date,1,@direction,transfer_mode
               from ftp_endpoint where xx=-@xx
    end
end
go



drop function sf_get_week_start
go
create function sf_get_week_start()
returns datetimeoffset
as begin
    declare @nowlocal datetime = getdate()

    return cast(cast(dateadd(day,1-datepart(dw,@nowlocal),@nowlocal) as date) as datetime) at time zone 'Central European Standard Time'
end
go

drop function sf_get_week_minute
go
create function sf_get_week_minute()
returns smallint
as begin
    return datediff(mi,ftp.sf_get_week_start(),getdate())
end
go

drop function sf_get_next_sync
go
create function sf_get_next_sync (
    @xx int
)
returns datetimeoffset
as begin
    declare @week_start datetimeoffset, @ret datetimeoffset,
            @week_minute smallint

     select @week_start=ftp.sf_get_week_start(),
            @week_minute=ftp.sf_get_week_minute()

    select @ret=case
        when @week_minute<job_start then dateadd(minute,job_start,@week_start)
        else dateadd(minute,ceiling(cast(@week_minute - job_start as float) / job_stride) * job_stride,@week_start)
       end
      from ftp_schedule where xx=@xx

    select @ret=case when aux.start<@ret and aux.missed<@ret then aux.missed else @ret end
      from (select dateadd(minute,job_start,@week_start) as start,dateadd(minute,job_stride,refresh_date) as missed
              from ftp_endpoint fe,ftp_schedule fs
             where fe.xx=fs.end_xx and fs.xx=@xx) aux

    return @ret
end
go
