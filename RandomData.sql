DELIMITER $$
CREATE DEFINER=`root`@`localhost` PROCEDURE `InsertRandomAuthorizations`()
BEGIN
    DECLARE i INT DEFAULT 0;
    DECLARE random_login VARCHAR(45);
    DECLARE random_pass VARCHAR(45);
    DECLARE random_role VARCHAR(45);
    DECLARE random_phone VARCHAR(50);
    DECLARE random_email VARCHAR(100);
    DECLARE random_active TINYINT(1);

    WHILE i < 1000 DO
        -- Generate random data
        SET random_login = CONCAT('user', FLOOR(RAND() * 1000000));
        SET random_pass = CONCAT('pass', FLOOR(RAND() * 1000000));
        SET random_role = ELT(FLOOR(1 + RAND() * 4), 'admin', 'manager', 'user', 'guest');
        SET random_phone = CONCAT('+', FLOOR(1 + RAND() * 99), FLOOR(100000000 + RAND() * 900000000));
        SET random_email = CONCAT('email', FLOOR(RAND() * 1000000), '@example.com');
        SET random_active = FLOOR(RAND() * 2);

        -- Insert record
        INSERT INTO `authorization` (
            `Login`, `Password`, `RoleName`,
            `Phone`, `Email`, `IsActive`
        ) VALUES (
            random_login, random_pass, random_role,
            random_phone, random_email, random_active
        );

        SET i = i + 1;

        -- Progress indicator (every 100 records)
        IF i % 100 = 0 THEN
            SELECT CONCAT('Inserted ', i, ' records') AS progress;
        END IF;
    END WHILE;

    SELECT 'Completed: 1000 records inserted' AS result;
END$$
DELIMITER ;
