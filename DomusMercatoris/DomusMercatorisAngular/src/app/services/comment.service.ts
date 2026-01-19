import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Comment, CreateCommentDto } from '../models/comment.model';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CommentService {
  private http = inject(HttpClient);
  
  private get apiUrl(): string {
    return `/api/comments`;
  }

  comments = signal<Comment[]>([]);

  fetchComments(productId: number): void {
    this.http.get<Comment[]>(`${this.apiUrl}/product/${productId}`)
      .subscribe({
        next: (data) => this.comments.set(data),
        error: (err) => console.error('Failed to fetch comments', err)
      });
  }

  createComment(dto: CreateCommentDto): Observable<Comment> {
    return this.http.post<Comment>(this.apiUrl, dto).pipe(
      tap(() => this.fetchComments(dto.productId))
    );
  }

  clearComments() {
    this.comments.set([]);
  }
}
