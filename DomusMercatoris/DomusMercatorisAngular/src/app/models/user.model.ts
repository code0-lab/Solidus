export interface User {
    email: string;
    token: string;
    firstName?: string;
    lastName?: string;
}

export interface LoginResponse {
    token: string;
    user: User;
}
