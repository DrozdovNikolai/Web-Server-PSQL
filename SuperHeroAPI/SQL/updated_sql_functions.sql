-- FUNCTION: public.add_global_permission_to_role()
CREATE OR REPLACE FUNCTION public.add_global_permission_to_role()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    role_name TEXT;
BEGIN
    -- Получаем имя роли на основе role_id
    SELECT role_name INTO role_name FROM ums_roles WHERE role_id = NEW.role_id;

    -- Выдаём права на основе значений флагов
    IF NEW.create_table_grant THEN
        EXECUTE format('GRANT CREATE ON DATABASE %I TO %I', 'superherodb', role_name);
    END IF;

    IF NEW.update_table_grant THEN
        EXECUTE format('GRANT UPDATE ON ALL TABLES IN SCHEMA %I TO %I', 'public', role_name);
    END IF;

    IF NEW.delete_table_grant THEN
        EXECUTE format('GRANT DELETE ON ALL TABLES IN SCHEMA %I TO %I', 'public', role_name);
    END IF;

    IF NEW.create_grant THEN
        EXECUTE format('GRANT CREATE ON SCHEMA %I TO %I', 'public', role_name);
    END IF;

    RETURN NEW;
END;
$BODY$;

ALTER FUNCTION public.add_global_permission_to_role()
    OWNER TO postgres;

-- FUNCTION: public.add_permission_to_role()
CREATE OR REPLACE FUNCTION public.add_permission_to_role()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    role_name TEXT;
    table_name TEXT;
    column_name TEXT;
BEGIN
    -- Fetch the role name from the ums_roles table based on role_id
    SELECT role_name INTO role_name FROM ums_roles WHERE role_id = NEW.role_id;

    -- Check if the table_name contains a dot symbol
    IF POSITION('.' IN NEW.table_name) > 0 THEN
        -- Extract the table name and column name
        table_name := split_part(NEW.table_name, '.', 1);
        column_name := split_part(NEW.table_name, '.', 2);

        IF NEW.operation = 0 THEN
            -- Grant INSERT privilege on the specified column to the role
            EXECUTE format('GRANT INSERT (%I) ON TABLE %I TO %I', column_name, table_name, role_name);
        ELSIF NEW.operation = 1 THEN
            -- Grant SELECT privilege on the specified column to the role
            EXECUTE format('GRANT SELECT (%I) ON TABLE %I TO %I', column_name, table_name, role_name);
        ELSIF NEW.operation = 2 THEN
            -- Grant UPDATE privilege on the specified column to the role
            EXECUTE format('GRANT UPDATE (%I) ON TABLE %I TO %I', column_name, table_name, role_name);
        END IF;
    ELSE
        -- Standard table-level permissions
        table_name := NEW.table_name;

        IF NEW.operation = 0 THEN
            -- Grant INSERT privilege on the table
            EXECUTE format('GRANT INSERT ON TABLE %I TO %I', table_name, role_name);
        ELSIF NEW.operation = 1 THEN
            -- Grant SELECT privilege on the table
            EXECUTE format('GRANT SELECT ON TABLE %I TO %I', table_name, role_name);
        ELSIF NEW.operation = 2 THEN
            -- Grant UPDATE privilege on the table
            EXECUTE format('GRANT UPDATE ON TABLE %I TO %I', table_name, role_name);
        ELSIF NEW.operation = 3 THEN
            -- Grant DELETE privilege on the table
            EXECUTE format('GRANT DELETE ON TABLE %I TO %I', table_name, role_name);
        ELSIF NEW.operation = 4 THEN
            -- Grant ALTER privilege on the table
            EXECUTE format('GRANT ALTER ON TABLE %I TO %I', table_name, role_name);
        ELSIF NEW.operation = 5 THEN
            -- Grant DROP privilege on the table
            EXECUTE format('GRANT DROP ON TABLE %I TO %I', table_name, role_name);
        ELSIF NEW.operation = 6 THEN
            -- Grant CREATE TRIGGER privilege on the table
            EXECUTE format('GRANT TRIGGER ON TABLE %I TO %I', table_name, role_name);
        ELSIF NEW.operation = 7 THEN
            -- Grant CREATE INDEX privilege on the table
            EXECUTE format('GRANT CREATE ON TABLE %I TO %I', table_name, role_name);
        ELSIF NEW.operation = 8 THEN
            -- Grant VACUUM privilege (useful for table maintenance)
            EXECUTE format('GRANT VACUUM ON TABLE %I TO %I', table_name, role_name);
        ELSIF NEW.operation = 9 THEN
            -- Grant ANALYZE privilege (useful for gathering statistics)
            EXECUTE format('GRANT ANALYZE ON TABLE %I TO %I', table_name, role_name);
        ELSIF NEW.operation = 10 THEN
            -- Grant ALL privileges on the table
            EXECUTE format('GRANT ALL PRIVILEGES ON TABLE %I TO %I', table_name, role_name);
        END IF;
    END IF;

    RETURN NEW;
END;
$BODY$;

ALTER FUNCTION public.add_permission_to_role()
    OWNER TO postgres;

-- FUNCTION: public.create_role_from_roles_table()
CREATE OR REPLACE FUNCTION public.create_role_from_roles_table()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
BEGIN
    -- Check if the role_name is not null
    IF NEW.role_name IS NOT NULL THEN
        -- Create the role with the provided name
        EXECUTE format('CREATE ROLE %I', NEW.role_name);
    END IF;
    RETURN NEW;
END;
$BODY$;

ALTER FUNCTION public.create_role_from_roles_table()
    OWNER TO postgres;

-- FUNCTION: public.delete_global_permission_from_role()
CREATE OR REPLACE FUNCTION public.delete_global_permission_from_role()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    role_name TEXT;
BEGIN
    -- Получаем имя роли на основе role_id
    SELECT role_name INTO role_name FROM ums_roles WHERE role_id = OLD.role_id;

    -- Отзыв прав на основе значений флагов
    IF OLD.create_table_grant THEN
        EXECUTE format('REVOKE CREATE ON DATABASE %I FROM %I', 'superherodb', role_name);
    END IF;

    IF OLD.update_table_grant THEN
        EXECUTE format('REVOKE UPDATE ON ALL TABLES IN SCHEMA %I FROM %I', 'public', role_name);
    END IF;

    IF OLD.delete_table_grant THEN
        EXECUTE format('REVOKE DELETE ON ALL TABLES IN SCHEMA %I FROM %I', 'public', role_name);
    END IF;

    IF OLD.create_grant THEN
        EXECUTE format('REVOKE CREATE ON SCHEMA %I FROM %I', 'public', role_name);
    END IF;

    RETURN OLD;
END;
$BODY$;

ALTER FUNCTION public.delete_global_permission_from_role()
    OWNER TO postgres;

-- FUNCTION: public.delete_permission_from_role()
CREATE OR REPLACE FUNCTION public.delete_permission_from_role()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    role_name TEXT;
    table_name TEXT;
    column_name TEXT;
BEGIN
    -- Fetch the role name from the ums_roles table based on role_id
    SELECT role_name INTO role_name FROM ums_roles WHERE role_id = OLD.role_id;

    -- Revoke the permissions from the role for the deleted row
    IF POSITION('.' IN OLD.table_name) > 0 THEN
        -- Extract table name and column name if there is a dot
        table_name := split_part(OLD.table_name, '.', 1);
        column_name := split_part(OLD.table_name, '.', 2);

        IF OLD.operation = 0 THEN
            EXECUTE format('REVOKE INSERT (%I) ON TABLE %I FROM %I', column_name, table_name, role_name);
        ELSIF OLD.operation = 1 THEN
            EXECUTE format('REVOKE SELECT (%I) ON TABLE %I FROM %I', column_name, table_name, role_name);
        ELSIF OLD.operation = 2 THEN
            EXECUTE format('REVOKE UPDATE (%I) ON TABLE %I FROM %I', column_name, table_name, role_name);
        END IF;
    ELSE
        -- Standard table-level permissions
        IF OLD.operation = 0 THEN
            EXECUTE format('REVOKE INSERT ON TABLE %I FROM %I', OLD.table_name, role_name);
        ELSIF OLD.operation = 1 THEN
            EXECUTE format('REVOKE SELECT ON TABLE %I FROM %I', OLD.table_name, role_name);
        ELSIF OLD.operation = 2 THEN
            EXECUTE format('REVOKE UPDATE ON TABLE %I FROM %I', OLD.table_name, role_name);
        ELSIF OLD.operation = 3 THEN
            EXECUTE format('REVOKE DELETE ON TABLE %I FROM %I', OLD.table_name, role_name);
        ELSIF OLD.operation = 4 THEN
            EXECUTE format('REVOKE ALTER ON TABLE %I FROM %I', OLD.table_name, role_name);
        ELSIF OLD.operation = 5 THEN
            EXECUTE format('REVOKE DROP ON TABLE %I FROM %I', OLD.table_name, role_name);
        ELSIF OLD.operation = 6 THEN
            EXECUTE format('REVOKE TRIGGER ON TABLE %I FROM %I', OLD.table_name, role_name);
        ELSIF OLD.operation = 7 THEN
            EXECUTE format('REVOKE CREATE ON TABLE %I FROM %I', OLD.table_name, role_name);
        ELSIF OLD.operation = 8 THEN
            EXECUTE format('REVOKE VACUUM ON TABLE %I FROM %I', OLD.table_name, role_name);
        ELSIF OLD.operation = 9 THEN
            EXECUTE format('REVOKE ANALYZE ON TABLE %I FROM %I', OLD.table_name, role_name);
        ELSIF OLD.operation = 10 THEN
            EXECUTE format('REVOKE ALL PRIVILEGES ON TABLE %I FROM %I', OLD.table_name, role_name);
        END IF;
    END IF;

    RETURN OLD;
END;
$BODY$;

ALTER FUNCTION public.delete_permission_from_role()
    OWNER TO postgres;

-- FUNCTION: public.delete_role_from_roles_table()
CREATE OR REPLACE FUNCTION public.delete_role_from_roles_table()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
BEGIN
    -- Drop the role if it exists
    EXECUTE format('DROP ROLE IF EXISTS %I', OLD.role_name);
    RETURN OLD;
END;
$BODY$;

ALTER FUNCTION public.delete_role_from_roles_table()
    OWNER TO postgres;

-- FUNCTION: public.update_global_role_permissions()
CREATE OR REPLACE FUNCTION public.update_global_role_permissions()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    role_name TEXT;
BEGIN
    -- Получаем имя роли на основе role_id
    SELECT role_name INTO role_name FROM ums_roles WHERE role_id = NEW.role_id;

    -- Если изменился create_table_grant
    IF OLD.create_table_grant <> NEW.create_table_grant THEN
        IF OLD.create_table_grant THEN
            EXECUTE format('REVOKE CREATE ON DATABASE %I FROM %I', 'superherodb', role_name);
        END IF;
        IF NEW.create_table_grant THEN
            EXECUTE format('GRANT CREATE ON DATABASE %I TO %I', 'superherodb', role_name);
        END IF;
    END IF;

    -- Если изменился update_table_grant
    IF OLD.update_table_grant <> NEW.update_table_grant THEN
        IF OLD.update_table_grant THEN
            EXECUTE format('REVOKE UPDATE ON ALL TABLES IN SCHEMA %I FROM %I', 'public', role_name);
        END IF;
        IF NEW.update_table_grant THEN
            EXECUTE format('GRANT UPDATE ON ALL TABLES IN SCHEMA %I TO %I', 'public', role_name);
        END IF;
    END IF;

    -- Если изменился delete_table_grant
    IF OLD.delete_table_grant <> NEW.delete_table_grant THEN
        IF OLD.delete_table_grant THEN
            EXECUTE format('REVOKE DELETE ON ALL TABLES IN SCHEMA %I FROM %I', 'public', role_name);
        END IF;
        IF NEW.delete_table_grant THEN
            EXECUTE format('GRANT DELETE ON ALL TABLES IN SCHEMA %I TO %I', 'public', role_name);
        END IF;
    END IF;

    -- Если изменился create_grant
    IF OLD.create_grant <> NEW.create_grant THEN
        IF OLD.create_grant THEN
            EXECUTE format('REVOKE CREATE ON SCHEMA %I FROM %I', 'public', role_name);
        END IF;
        IF NEW.create_grant THEN
            EXECUTE format('GRANT CREATE ON SCHEMA %I TO %I', 'public', role_name);
        END IF;
    END IF;

    RETURN NEW;
END;
$BODY$;

ALTER FUNCTION public.update_global_role_permissions()
    OWNER TO postgres;

-- FUNCTION: public.update_role_from_roles_table()
CREATE OR REPLACE FUNCTION public.update_role_from_roles_table()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
BEGIN
    -- Check if the role_name has been modified
    IF OLD.role_name <> NEW.role_name THEN
        -- Check if the old role exists
        PERFORM 1 FROM pg_roles WHERE rolname = OLD.role_name;
        IF FOUND THEN
            -- Rename the old role to the new role
            EXECUTE format('ALTER ROLE %I RENAME TO %I', OLD.role_name, NEW.role_name);
        END IF;
    END IF;
    RETURN NEW;
END;
$BODY$;

ALTER FUNCTION public.update_role_from_roles_table()
    OWNER TO postgres;

-- FUNCTION: public.update_role_permissions()
CREATE OR REPLACE FUNCTION public.update_role_permissions()
    RETURNS trigger
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE NOT LEAKPROOF
AS $BODY$
DECLARE
    role_name TEXT;
    table_name TEXT;
    column_name TEXT;
BEGIN
    -- Fetch the role name from the ums_roles table based on role_id
    SELECT role_name INTO role_name FROM ums_roles WHERE role_id = NEW.role_id;

    -- Only proceed if the operation or table has changed
    IF OLD.operation <> NEW.operation OR OLD.table_name <> NEW.table_name THEN
        -- Revoke the old permissions from the role
        IF POSITION('.' IN OLD.table_name) > 0 THEN
            table_name := split_part(OLD.table_name, '.', 1);
            column_name := split_part(OLD.table_name, '.', 2);

            IF OLD.operation = 0 THEN
                EXECUTE format('REVOKE INSERT (%I) ON TABLE %I FROM %I', column_name, table_name, role_name);
            ELSIF OLD.operation = 1 THEN
                EXECUTE format('REVOKE SELECT (%I) ON TABLE %I FROM %I', column_name, table_name, role_name);
            ELSIF OLD.operation = 2 THEN
                EXECUTE format('REVOKE UPDATE (%I) ON TABLE %I FROM %I', column_name, table_name, role_name);
            ELSIF OLD.operation = 3 THEN
                EXECUTE format('REVOKE DELETE (%I) ON TABLE %I FROM %I', column_name, table_name, role_name);
            ELSIF OLD.operation = 4 THEN
                EXECUTE format('REVOKE ALTER ON TABLE %I FROM %I', table_name, role_name);
            ELSIF OLD.operation = 5 THEN
                EXECUTE format('REVOKE DROP ON TABLE %I FROM %I', table_name, role_name);
            ELSIF OLD.operation = 6 THEN
                EXECUTE format('REVOKE TRIGGER ON TABLE %I FROM %I', table_name, role_name);
            ELSIF OLD.operation = 7 THEN
                EXECUTE format('REVOKE CREATE ON TABLE %I FROM %I', table_name, role_name);
            ELSIF OLD.operation = 8 THEN
                EXECUTE format('REVOKE VACUUM ON TABLE %I FROM %I', table_name, role_name);
            ELSIF OLD.operation = 9 THEN
                EXECUTE format('REVOKE ANALYZE ON TABLE %I FROM %I', table_name, role_name);
            ELSIF OLD.operation = 10 THEN
                EXECUTE format('REVOKE ALL PRIVILEGES ON TABLE %I FROM %I', table_name, role_name);
            END IF;

        ELSE
            -- Standard table-level permissions
            IF OLD.operation = 0 THEN
                EXECUTE format('REVOKE INSERT ON TABLE %I FROM %I', OLD.table_name, role_name);
            ELSIF OLD.operation = 1 THEN
                EXECUTE format('REVOKE SELECT ON TABLE %I FROM %I', OLD.table_name, role_name);
            ELSIF OLD.operation = 2 THEN
                EXECUTE format('REVOKE UPDATE ON TABLE %I FROM %I', OLD.table_name, role_name);
            ELSIF OLD.operation = 3 THEN
                EXECUTE format('REVOKE DELETE ON TABLE %I FROM %I', OLD.table_name, role_name);
            ELSIF OLD.operation = 4 THEN
                EXECUTE format('REVOKE ALTER ON TABLE %I FROM %I', OLD.table_name, role_name);
            ELSIF OLD.operation = 5 THEN
                EXECUTE format('REVOKE DROP ON TABLE %I FROM %I', OLD.table_name, role_name);
            ELSIF OLD.operation = 6 THEN
                EXECUTE format('REVOKE TRIGGER ON TABLE %I FROM %I', OLD.table_name, role_name);
            ELSIF OLD.operation = 7 THEN
                EXECUTE format('REVOKE CREATE ON TABLE %I FROM %I', OLD.table_name, role_name);
            ELSIF OLD.operation = 8 THEN
                EXECUTE format('REVOKE VACUUM ON TABLE %I FROM %I', OLD.table_name, role_name);
            ELSIF OLD.operation = 9 THEN
                EXECUTE format('REVOKE ANALYZE ON TABLE %I FROM %I', OLD.table_name, role_name);
            ELSIF OLD.operation = 10 THEN
                EXECUTE format('REVOKE ALL PRIVILEGES ON TABLE %I FROM %I', OLD.table_name, role_name);
            END IF;
        END IF;

        -- Grant the updated permissions to the role
        IF POSITION('.' IN NEW.table_name) > 0 THEN
            table_name := split_part(NEW.table_name, '.', 1);
            column_name := split_part(NEW.table_name, '.', 2);

            IF NEW.operation = 0 THEN
                EXECUTE format('GRANT INSERT (%I) ON TABLE %I TO %I', column_name, table_name, role_name);
            ELSIF NEW.operation = 1 THEN
                EXECUTE format('GRANT SELECT (%I) ON TABLE %I TO %I', column_name, table_name, role_name);
            ELSIF NEW.operation = 2 THEN
                EXECUTE format('GRANT UPDATE (%I) ON TABLE %I TO %I', column_name, table_name, role_name);
            ELSIF NEW.operation = 3 THEN
                EXECUTE format('GRANT DELETE (%I) ON TABLE %I TO %I', column_name, table_name, role_name);
            ELSIF NEW.operation = 4 THEN
                EXECUTE format('GRANT ALTER ON TABLE %I TO %I', table_name, role_name);
            ELSIF NEW.operation = 5 THEN
                EXECUTE format('GRANT DROP ON TABLE %I TO %I', table_name, role_name);
            ELSIF NEW.operation = 6 THEN
                EXECUTE format('GRANT TRIGGER ON TABLE %I TO %I', table_name, role_name);
            ELSIF NEW.operation = 7 THEN
                EXECUTE format('GRANT CREATE ON TABLE %I TO %I', table_name, role_name);
            ELSIF NEW.operation = 8 THEN
                EXECUTE format('GRANT VACUUM ON TABLE %I TO %I', table_name, role_name);
            ELSIF NEW.operation = 9 THEN
                EXECUTE format('GRANT ANALYZE ON TABLE %I TO %I', table_name, role_name);
            ELSIF NEW.operation = 10 THEN
                EXECUTE format('GRANT ALL PRIVILEGES ON TABLE %I TO %I', table_name, role_name);
            END IF;

        ELSE
            -- Standard table-level permissions
            IF NEW.operation = 0 THEN
                EXECUTE format('GRANT INSERT ON TABLE %I TO %I', NEW.table_name, role_name);
            ELSIF NEW.operation = 1 THEN
                EXECUTE format('GRANT SELECT ON TABLE %I TO %I', NEW.table_name, role_name);
            ELSIF NEW.operation = 2 THEN
                EXECUTE format('GRANT UPDATE ON TABLE %I TO %I', NEW.table_name, role_name);
            ELSIF NEW.operation = 3 THEN
                EXECUTE format('GRANT DELETE ON TABLE %I TO %I', NEW.table_name, role_name);
            ELSIF NEW.operation = 4 THEN
                EXECUTE format('GRANT ALTER ON TABLE %I TO %I', NEW.table_name, role_name);
            ELSIF NEW.operation = 5 THEN
                EXECUTE format('GRANT DROP ON TABLE %I TO %I', NEW.table_name, role_name);
            ELSIF NEW.operation = 6 THEN
                EXECUTE format('GRANT TRIGGER ON TABLE %I TO %I', NEW.table_name, role_name);
            ELSIF NEW.operation = 7 THEN
                EXECUTE format('GRANT CREATE ON TABLE %I TO %I', NEW.table_name, role_name);
            ELSIF NEW.operation = 8 THEN
                EXECUTE format('GRANT VACUUM ON TABLE %I TO %I', NEW.table_name, role_name);
            ELSIF NEW.operation = 9 THEN
                EXECUTE format('GRANT ANALYZE ON TABLE %I TO %I', NEW.table_name, role_name);
            ELSIF NEW.operation = 10 THEN
                EXECUTE format('GRANT ALL PRIVILEGES ON TABLE %I TO %I', NEW.table_name, role_name);
            END IF;
        END IF;
    END IF;

    RETURN NEW;
END;
$BODY$;

ALTER FUNCTION public.update_role_permissions()
    OWNER TO postgres;

-- Создание или обновление триггеров для таблиц
CREATE OR REPLACE TRIGGER tr_create_role_from_roles_table
    AFTER INSERT
    ON ums_roles
    FOR EACH ROW
    EXECUTE FUNCTION create_role_from_roles_table();

CREATE OR REPLACE TRIGGER tr_update_role_from_roles_table
    AFTER UPDATE
    ON ums_roles
    FOR EACH ROW
    EXECUTE FUNCTION update_role_from_roles_table();

CREATE OR REPLACE TRIGGER tr_delete_role_from_roles_table
    BEFORE DELETE
    ON ums_roles
    FOR EACH ROW
    EXECUTE FUNCTION delete_role_from_roles_table();

CREATE OR REPLACE TRIGGER tr_add_permission_to_role
    AFTER INSERT
    ON ums_permissions
    FOR EACH ROW
    EXECUTE FUNCTION add_permission_to_role();

CREATE OR REPLACE TRIGGER tr_update_role_permissions
    AFTER UPDATE
    ON ums_permissions
    FOR EACH ROW
    EXECUTE FUNCTION update_role_permissions();

CREATE OR REPLACE TRIGGER tr_delete_permission_from_role
    BEFORE DELETE
    ON ums_permissions
    FOR EACH ROW
    EXECUTE FUNCTION delete_permission_from_role();

CREATE OR REPLACE TRIGGER tr_add_global_permission_to_role
    AFTER INSERT
    ON ums_global_permissions
    FOR EACH ROW
    EXECUTE FUNCTION add_global_permission_to_role();

CREATE OR REPLACE TRIGGER tr_update_global_role_permissions
    AFTER UPDATE
    ON ums_global_permissions
    FOR EACH ROW
    EXECUTE FUNCTION update_global_role_permissions();

CREATE OR REPLACE TRIGGER tr_delete_global_permission_from_role
    BEFORE DELETE
    ON ums_global_permissions
    FOR EACH ROW
    EXECUTE FUNCTION delete_global_permission_from_role(); 