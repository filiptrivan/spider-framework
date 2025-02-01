import { BaseEntity } from "./base-entity";
import { User } from "./security-entities";

export class InitTopBarData extends BaseEntity
{
    companyName?: string;
    userProfilePath?: string;
    unreadNotificationsCount?: number;
    showProfileIcon?: boolean;
    currentUser?: User;
  
    constructor(
    {
        companyName,
        userProfilePath,
        unreadNotificationsCount,
        showProfileIcon,
        currentUser,
    }:{
        companyName?: string,
        userProfilePath?: string,
        unreadNotificationsCount?: number,
        showProfileIcon?: boolean,
        currentUser?: User,
    } = {}
    ) {
        super('InitTopBarData');

        this.companyName = companyName;
        this.userProfilePath = userProfilePath;
        this.unreadNotificationsCount = unreadNotificationsCount;
        this.showProfileIcon = showProfileIcon;
        this.currentUser = currentUser;
    }
}