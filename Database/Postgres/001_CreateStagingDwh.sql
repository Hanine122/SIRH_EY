/* ============================================================================
   SIRH.EY — Analytics Layer (PostgreSQL)
   Provisions the staging / data-warehouse split.

   Two deployment options are covered below — use ONE of them depending on
   your server topology:

     OPTION A — Separate databases (sirh_staging, sirh_dwh)
       Use this if staging and DWH are meant to be isolated at the connection
       level (different credentials, different backup/retention policies,
       or hosted on different PostgreSQL clusters).

     OPTION B — Single database, two schemas (staging, dwh)
       Use this if both layers live on the same PostgreSQL server/instance.
       This is the simpler default for most single-server deployments and
       keeps cross-schema joins (staging.* -> dwh.*) possible without
       dblink/fdw.

   Only run the block matching your actual topology.
   ============================================================================ */


/* ============================================================================
   OPTION A — Separate databases
   CREATE DATABASE cannot run inside a transaction block and cannot be
   combined with other statements in a single execution when using psql's
   default transactional wrapping; run each CREATE DATABASE individually
   (e.g. via `psql -c "..."`) or as standalone statements, not inside BEGIN/COMMIT.
   ============================================================================ */

CREATE DATABASE sirh_staging
    WITH ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE   = 'en_US.UTF-8'
    TEMPLATE   = template0;

CREATE DATABASE sirh_dwh
    WITH ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE   = 'en_US.UTF-8'
    TEMPLATE   = template0;

-- Optional: dedicated owners per database (adjust roles/passwords as needed)
-- ALTER DATABASE sirh_staging OWNER TO sirh_staging_owner;
-- ALTER DATABASE sirh_dwh     OWNER TO sirh_dwh_owner;


/* ============================================================================
   OPTION B — Same server, two schemas
   Run this against the single shared database (e.g. sirh).
   ============================================================================ */

CREATE SCHEMA IF NOT EXISTS staging;
CREATE SCHEMA IF NOT EXISTS dwh;

COMMENT ON SCHEMA staging IS 'Typed pass-through copies of OLTP source tables (1:1, no transformation).';
COMMENT ON SCHEMA dwh     IS 'Dimensional model (dims/facts) built from the staging layer.';

-- Optional: restrict search_path defaults / grants per role, e.g.:
-- GRANT USAGE ON SCHEMA staging TO sirh_etl_role;
-- GRANT USAGE ON SCHEMA dwh     TO sirh_bi_role;
-- ALTER ROLE sirh_etl_role SET search_path = staging, public;
-- ALTER ROLE sirh_bi_role  SET search_path = dwh, public;
