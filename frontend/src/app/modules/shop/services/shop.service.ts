import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface ProductModel {
  id: string;
  name: string;
  category: string;
  price: number;
  color: string;
  description: string;
  sizes: string[];
  imageUrl: string;
  imageUrls: string[];
  storeId: string;
  storeName: string;
  storeUrl: string;
  productUrl: string;
  rating: number;
  reviews: number;
  inStock: boolean;
  sku: string;
}

export interface ProductListResponse {
  data: ProductModel[];
  total: number;
}

@Injectable({
  providedIn: 'root'
})
export class ShopService {
  private apiUrl = environment.apiUrl + '/shop';

  constructor(private http: HttpClient) { }

  getProducts(category?: string, minPrice?: number, maxPrice?: number, page: number = 1, pageSize: number = 20): Observable<ProductListResponse> {
    const params = new URLSearchParams();
    if (category) { params.append('category', category); }
    if (minPrice !== undefined && minPrice !== null) { params.append('minPrice', minPrice.toString()); }
    if (maxPrice !== undefined && maxPrice !== null) { params.append('maxPrice', maxPrice.toString()); }
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());

    return this.http.get<ProductListResponse>(`${this.apiUrl}/products?${params.toString()}`);
  }

  getProductById(id: string | null): Observable<ProductModel> {
    return this.http.get<ProductModel>(`${this.apiUrl}/products/${id}`);
  }

  createOrder(orderData: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/orders`, orderData);
  }
}
