--------------------------------------------------------
--  File created - pi�tek-kwietnia-17-2020   
--------------------------------------------------------
--------------------------------------------------------
--  DDL for Table FTP_FILE
--------------------------------------------------------

  CREATE TABLE "FTPGETWORK"."FTP_FILE" 
   (	"XX" NUMBER, 
	"END_XX" NUMBER, 
	"SCH_XX" NUMBER, 
	"FILE_NAME" VARCHAR2(128 BYTE), 
	"FILE_SIZE" NUMBER, 
	"FILE_DATE" DATE, 
	"COMPLETION_DATE" DATE DEFAULT sysdate, 
	"MANUAL_SYNC" NUMBER(1,0), 
	"DIRECTION" NUMBER(1,0), 
	"TRANSFER_MODE" CHAR(1 BYTE)
   ) SEGMENT CREATION IMMEDIATE 
  PCTFREE 10 PCTUSED 40 INITRANS 1 MAXTRANS 255 
 NOCOMPRESS LOGGING
  STORAGE(INITIAL 65536 NEXT 1048576 MINEXTENTS 1 MAXEXTENTS 2147483645
  PCTINCREASE 0 FREELISTS 1 FREELIST GROUPS 1
  BUFFER_POOL DEFAULT FLASH_CACHE DEFAULT CELL_FLASH_CACHE DEFAULT)
  TABLESPACE "USERS" ;
--------------------------------------------------------
--  DDL for Index PK_FTP_FILE
--------------------------------------------------------

  CREATE UNIQUE INDEX "FTPGETWORK"."PK_FTP_FILE" ON "FTPGETWORK"."FTP_FILE" ("XX") 
  PCTFREE 10 INITRANS 2 MAXTRANS 255 COMPUTE STATISTICS 
  STORAGE(INITIAL 65536 NEXT 1048576 MINEXTENTS 1 MAXEXTENTS 2147483645
  PCTINCREASE 0 FREELISTS 1 FREELIST GROUPS 1
  BUFFER_POOL DEFAULT FLASH_CACHE DEFAULT CELL_FLASH_CACHE DEFAULT)
  TABLESPACE "USERS" ;
--------------------------------------------------------
--  Constraints for Table FTP_FILE
--------------------------------------------------------

  ALTER TABLE "FTPGETWORK"."FTP_FILE" MODIFY ("XX" NOT NULL ENABLE);
  ALTER TABLE "FTPGETWORK"."FTP_FILE" MODIFY ("FILE_NAME" NOT NULL ENABLE);
  ALTER TABLE "FTPGETWORK"."FTP_FILE" MODIFY ("FILE_SIZE" NOT NULL ENABLE);
  ALTER TABLE "FTPGETWORK"."FTP_FILE" MODIFY ("FILE_DATE" NOT NULL ENABLE);
  ALTER TABLE "FTPGETWORK"."FTP_FILE" MODIFY ("COMPLETION_DATE" NOT NULL ENABLE);
  ALTER TABLE "FTPGETWORK"."FTP_FILE" ADD CONSTRAINT "CK_START_MODE" CHECK ((manual_sync is null and sch_xx is not null) or (manual_sync is not null and end_xx is not null)) ENABLE;
  ALTER TABLE "FTPGETWORK"."FTP_FILE" ADD CONSTRAINT "PK_FTP_FILE" PRIMARY KEY ("XX")
  USING INDEX PCTFREE 10 INITRANS 2 MAXTRANS 255 COMPUTE STATISTICS 
  STORAGE(INITIAL 65536 NEXT 1048576 MINEXTENTS 1 MAXEXTENTS 2147483645
  PCTINCREASE 0 FREELISTS 1 FREELIST GROUPS 1
  BUFFER_POOL DEFAULT FLASH_CACHE DEFAULT CELL_FLASH_CACHE DEFAULT)
  TABLESPACE "USERS"  ENABLE;
--------------------------------------------------------
--  Ref Constraints for Table FTP_FILE
--------------------------------------------------------

  ALTER TABLE "FTPGETWORK"."FTP_FILE" ADD CONSTRAINT "FK_FILE_ENDPOINT" FOREIGN KEY ("END_XX")
	  REFERENCES "FTPGETWORK"."FTP_ENDPOINT" ("XX") ENABLE;
  ALTER TABLE "FTPGETWORK"."FTP_FILE" ADD CONSTRAINT "FK_FILE_SCHEDULE" FOREIGN KEY ("SCH_XX")
	  REFERENCES "FTPGETWORK"."FTP_SCHEDULE" ("XX") ENABLE;
--------------------------------------------------------
--  DDL for Trigger BI_FTP_FILE
--------------------------------------------------------

  CREATE OR REPLACE EDITIONABLE TRIGGER "FTPGETWORK"."BI_FTP_FILE" before insert on ftp_file
for each row begin
  if :new.xx is null then
    select ftp_file_xx.nextval into :new.xx from dual;
  end if;
end;


/
ALTER TRIGGER "FTPGETWORK"."BI_FTP_FILE" ENABLE;
