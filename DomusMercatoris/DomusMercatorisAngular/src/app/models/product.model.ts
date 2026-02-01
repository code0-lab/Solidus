export interface Product {
    id: number;
    companyId: number;
    name: string;
    price: number;
    priceText?: string;
    images?: string[];
    imageUrls?: string[];
    imageUrl?: string;
    bg?: string;
    description?: string;
    variants?: VariantProduct[];
    isBlockedByCompany?: boolean;
}

export interface VariantProduct {
    id: number;
    productId: number;
    productName: string;
    color: string;
    price: number;
    coverImage?: string;
    isCustomizable: boolean;
}

export interface Category {
    id: number;
    name: string;
}

export interface Brand {
    id: number;
    name: string;
    description?: string;
}

export interface Company {
    companyId: number;
    name: string;
}

export interface PaginatedResult<T> {
    items: T[];
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
}
