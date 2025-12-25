export interface Comment {
    id: number;
    text: string;
    createdAt: string;
    userFullName: string;
    isApproved: boolean;
    productId: number;
}

export interface CreateCommentDto {
    productId: number;
    text: string;
}
