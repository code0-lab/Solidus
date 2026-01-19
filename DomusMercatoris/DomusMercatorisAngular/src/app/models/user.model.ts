export interface User {
    id?: number;
    email: string;
    token: string;
    firstName?: string;
    lastName?: string;
    phone?: string | null;
    address?: string | null;
}

export interface UserProfileDto {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
    companyId: number;
    phone?: string | null;
    address?: string | null;
    roles: string[];
}

export interface LoginResponse {
    token: string;
    user: UserProfileDto;
}
