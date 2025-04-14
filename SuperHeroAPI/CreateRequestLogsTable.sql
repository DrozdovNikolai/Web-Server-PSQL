-- Create RequestLogs table
CREATE TABLE IF NOT EXISTS ums_request_logs (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NULL,
    path TEXT NOT NULL,
    method VARCHAR(10) NOT NULL,
    query_string TEXT NOT NULL,
    request_body TEXT NOT NULL,
    response_body TEXT NOT NULL,
    status_code INTEGER NOT NULL,
    request_time TIMESTAMP NOT NULL,
    response_time TIMESTAMP NOT NULL,
    duration INTERVAL NOT NULL,
    ip_address VARCHAR(45) NOT NULL,
    CONSTRAINT fk_user FOREIGN KEY (user_id) REFERENCES ums_users (id)
);

-- Create index for faster user lookups
CREATE INDEX IF NOT EXISTS idx_request_logs_user_id ON ums_request_logs (user_id);

-- Create index for faster path lookups
CREATE INDEX IF NOT EXISTS idx_request_logs_path ON ums_request_logs (path);

-- Create index for status code lookups
CREATE INDEX IF NOT EXISTS idx_request_logs_status_code ON ums_request_logs (status_code);

-- Create index for request time lookups
CREATE INDEX IF NOT EXISTS idx_request_logs_request_time ON ums_request_logs (request_time); 