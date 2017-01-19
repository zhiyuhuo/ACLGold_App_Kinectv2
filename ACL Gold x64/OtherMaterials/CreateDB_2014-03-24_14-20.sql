/** ********************************
  * ACLGold Database Creation Script
  *
  * Author:  Brad Harris
  * Created: 13 MAR 2014
  * Updated: 24 MAR 2014
  *
  * Creates all tables & views necessary for normal data
  * management of the ACL Gold research project.
  */



/** **************************
  * Clean up previous versions
  */
DROP TABLE IF EXISTS `aclgold`.`ACLInjuryDuringStudy`;
DROP TABLE IF EXISTS `aclgold`.`FollowUps`;
DROP TABLE IF EXISTS `aclgold`.`UserInfo`;
DROP TABLE IF EXISTS `aclgold`.`Users`;
DROP TABLE IF EXISTS `aclgold`.`Subjects`;
DROP TABLE IF EXISTS `aclgold`.`Sports`;
DROP VIEW  IF EXISTS `aclgold`.`ShowAllSubjectData`;


/** ******************************
  * Create list of possible sports
  */
CREATE TABLE `aclgold`.`Sports` (
  `Id`          int(11)       NOT NULL AUTO_INCREMENT,
  `Name`        varchar(100)  NOT NULL,
  `Description` varchar(1000) NOT NULL DEFAULT '',
  PRIMARY KEY (`Id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8;


/** *****************************
  * Create table for subject data
  */
CREATE TABLE `aclgold`.`Subjects` (

  `Id`                        int(11) NOT NULL AUTO_INCREMENT,
  `DOB`                       date    NOT NULL DEFAULT '1900-01-01',

  `SportId1`                  int(11) NOT NULL DEFAULT 0 REFERENCES `aclgold`.`Sports`(`Id`),
  `SportId2`                  int(11) NOT NULL DEFAULT 0 REFERENCES `aclgold`.`Sports`(`Id`),
  `SportId3`                  int(11) NOT NULL DEFAULT 0 REFERENCES `aclgold`.`Sports`(`Id`),
  `SportId4`                  int(11) NOT NULL DEFAULT 0 REFERENCES `aclgold`.`Sports`(`Id`),
  `SportId5`                  int(11) NOT NULL DEFAULT 0 REFERENCES `aclgold`.`Sports`(`Id`),

  `PriorACLTear1Date`         date        NULL DEFAULT NULL,
  `PriorACLTear1Left`         bit     NOT NULL DEFAULT 0,
  `PriorACLTear1Right`        bit     NOT NULL DEFAULT 0,
  `PriorACLTear1Surgery`      bit     NOT NULL DEFAULT 0,

  `PriorACLTear2Date`         date        NULL DEFAULT NULL,
  `PriorACLTear2Left`         bit     NOT NULL DEFAULT 0,
  `PriorACLTear2Right`        bit     NOT NULL DEFAULT 0,
  `PriorACLTear2Surgery`      bit     NOT NULL DEFAULT 0,

  `PriorACLTear3Date`         date        NULL DEFAULT NULL,
  `PriorACLTear3Left`         bit     NOT NULL DEFAULT 0,
  `PriorACLTear3Right`        bit     NOT NULL DEFAULT 0,
  `PriorACLTear3Surgery`      bit     NOT NULL DEFAULT 0,

  `PostACLTearDate`           date        NULL DEFAULT NULL,
  `PostACLTearLeft`           bit     NOT NULL DEFAULT 0,
  `PostACLTearRight`          bit     NOT NULL DEFAULT 0,
  `PostACLTearSurgery`        bit     NOT NULL DEFAULT 0,
  `PostSportIdDuringInjury`   int(11) NOT NULL DEFAULT 0 REFERENCES `aclgold`.`Sports`(`Id`),

  `06MOFollowUpLatestAttempt` date        NULL DEFAULT NULL,
  `06MOFollowUpFailedCount`   int(11) NOT NULL DEFAULT 0,
  `06MOFollowUpSuccessful`    bit     NOT NULL DEFAULT 0,

  `12MOFollowUpLatestAttempt` date        NULL DEFAULT NULL,
  `12MOFollowUpFailedCount`   int(11) NOT NULL DEFAULT 0,
  `12MOFollowUpSuccessful`    bit     NOT NULL DEFAULT 0,

  PRIMARY KEY (`Id`)
) ENGINE=MyISAM DEFAULT CHARSET=utf8;


/** ****************************************************************
  * Create view to display all subject data in human-readable format
  */

CREATE VIEW `aclgold`.`ShowAllSubjectData` AS
  SELECT

     su.`Id`                                      AS `Subject Id`,
     su.`DOB`                                     AS `DOB`,
     FLOOR(DATEDIFF(CURDATE(),su.`DOB`) / 365.25) AS `Menarch`,

    sp1.`Name`                                    AS `Sport 1`,
    sp2.`Name`                                    AS `Sport 2`,
    sp3.`Name`                                    AS `Sport 3`,
    sp4.`Name`                                    AS `Sport 4`,
    sp5.`Name`                                    AS `Sport 5`,

     su.`PriorACLTear1Date`                       AS `Prior ACL Tear 1 Date`,
     IF(su.`PriorACLTear1Left`, 'Y', 'N')         AS `Prior ACL Tear 1 Left`,
     IF(su.`PriorACLTear1Right`, 'Y', 'N')        AS `Prior ACL Tear 1 Right`,
     IF(su.`PriorACLTear1Surgery`, 'Y', 'N')      AS `Prior ACL Tear 1 Surgery`,

     su.`PriorACLTear2Date`                       AS `Prior ACL Tear 2 Date`,
     IF(su.`PriorACLTear2Left`, 'Y', 'N')         AS `Prior ACL Tear 2 Left`,
     IF(su.`PriorACLTear2Right`, 'Y', 'N')        AS `Prior ACL Tear 2 Right`,
     IF(su.`PriorACLTear2Surgery`, 'Y', 'N')      AS `Prior ACL Tear 2 Surgery`,

     su.`PriorACLTear3Date`                       AS `Prior ACL Tear 3 Date`,
     IF(su.`PriorACLTear3Left`, 'Y', 'N')         AS `Prior ACL Tear 3 Left`,
     IF(su.`PriorACLTear3Right`, 'Y', 'N')        AS `Prior ACL Tear 3 Right`,
     IF(su.`PriorACLTear3Surgery`, 'Y', 'N')      AS `Prior ACL Tear 3 Surgery`,

     su.`PostACLTearDate`                         AS `Post ACL Tear Date`,
     IF(su.`PostACLTearLeft`, 'Y', 'N')           AS `Post ACL Tear Left`,
     IF(su.`PostACLTearRight`, 'Y', 'N')          AS `Post ACL Tear Right`,
     IF(su.`PostACLTearSurgery`, 'Y', 'N')        AS `Post ACL Tear Surgery`,
    sp6.`Name`                                    AS `Sport During Post-Test Injury`,

     su.`06MOFollowUpLatestAttempt`               AS `6-Month Follow-Up Latest Attempt`,
     su.`06MOFollowUpFailedCount`                 AS `6-Month Follow-Up Failed Attempt Count`,
     IF(su.`06MOFollowUpSuccessful`, 'Y', 'N')    AS `6-Month Follow-Up Successful`,

     su.`12MOFollowUpLatestAttempt`               AS `12-Month Follow-Up Latest Attempt`,
     su.`12MOFollowUpFailedCount`                 AS `12-Month Follow-Up Failed Attempt Count`,
     IF(su.`12MOFollowUpSuccessful`, 'Y', 'N')    AS `12-Month Follow-Up Successful`

  FROM      `aclgold`.`Subjects` su
  LEFT JOIN `aclgold`.`Sports`   sp1 ON su.`SportId1`                = sp1.`Id`
  LEFT JOIN `aclgold`.`Sports`   sp2 ON su.`SportId2`                = sp1.`Id`
  LEFT JOIN `aclgold`.`Sports`   sp3 ON su.`SportId3`                = sp1.`Id`
  LEFT JOIN `aclgold`.`Sports`   sp4 ON su.`SportId4`                = sp1.`Id`
  LEFT JOIN `aclgold`.`Sports`   sp5 ON su.`SportId5`                = sp1.`Id`
  LEFT JOIN `aclgold`.`Sports`   sp6 ON su.`PostSportIdDuringInjury` = sp1.`Id`
;


/** *********************************
  * Optionally add a little test data
  */

INSERT INTO `aclgold`.`Sports` ( `Name`, `Description` ) VALUES ( 'Baseball',     '' );
INSERT INTO `aclgold`.`Sports` ( `Name`, `Description` ) VALUES ( 'Basketball',   '' );
INSERT INTO `aclgold`.`Sports` ( `Name`, `Description` ) VALUES ( 'Football',     '' );
INSERT INTO `aclgold`.`Sports` ( `Name`, `Description` ) VALUES ( 'Tennis',       '' );

INSERT INTO `aclgold`.`Subjects` ( `DOB`, `SportId1`, `SportId2`, `PriorACLTear1Date`, `PriorACLTear1Left`, `PriorACLTear1Right`, `PriorACLTear1Surgery` ) VALUES ( '1964-03-27', 2, 0, '2012-08-12', 0, 1, 1 );


/** *************************************************
  * Display all subject data in human-readable format
  */

SELECT * FROM `aclgold`.`ShowAllSubjectData`;
