-- Volume Postgres já existente não reexecuta infra/postgres/init.sql.
CREATE SCHEMA IF NOT EXISTS arquivos;
GRANT ALL ON SCHEMA arquivos TO CURRENT_USER;
