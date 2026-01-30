import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface Membership {
  id: number;
  companyId: number;
  companyName: string;
  joinedAt: string;
}

export interface CompanySummary {
  id: number;
  name: string;
  isMember: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class MembershipService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/membership`;

  getMyMemberships(): Observable<Membership[]> {
    return this.http.get<Membership[]>(this.apiUrl);
  }

  joinCompany(companyId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${companyId}`, {});
  }

  leaveCompany(companyId: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${companyId}`);
  }

  searchCompanies(query: string): Observable<CompanySummary[]> {
    const params = new HttpParams().set('query', query);
    return this.http.get<CompanySummary[]>(`${this.apiUrl}/search`, { params });
  }
}
