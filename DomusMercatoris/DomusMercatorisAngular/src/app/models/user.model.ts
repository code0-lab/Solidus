export interface User {
    id?: number;
    email: string;
    token: string;
    firstName?: string;
    lastName?: string;
    phone?: string | null;
    address?: string | null;
    companyId?: number;
    roles: string[];
    profilePictureUrl?: string;
    blockedByCompanyIds?: number[];
}

export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
    companyId?: number;
}

export interface Company {
    companyId: number;
    name: string;
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
    profilePictureUrl?: string;
    blockedByCompanyIds?: number[];
}

export interface UpdateProfileRequest {
    firstName?: string;
    lastName?: string;
    email?: string;
    phone?: string | null;
    address?: string | null;
    currentPassword?: string;
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
    confirmNewPassword: string;
}

export interface ChangeEmailRequest {
    newEmail: string;
    currentPassword: string;
}

export interface LoginResponse {
    token: string;
    user: UserProfileDto;
}

export interface MyCompanyDto {
    id: number;
    name: string;
    isBlockedByMe: boolean;
}
