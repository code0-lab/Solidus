import { Component, Input, Output, EventEmitter, inject, signal, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Product } from '../../models/product.model';
import { CommentService } from '../../services/comment.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.css'
})
export class ProductDetailComponent implements OnChanges {
  @Input({ required: true }) product!: Product;
  @Output() onClose = new EventEmitter<void>();

  commentService = inject(CommentService);
  authService = inject(AuthService);

  activeTab: 'details' | 'comments' = 'details';
  newCommentText = signal('');
  currentImageIndex = signal(0);

  ngOnChanges(changes: SimpleChanges) {
    if (changes['product'] && this.product) {
      this.commentService.fetchComments(this.product.id);
      this.currentImageIndex.set(0);
    }
  }

  close() {
    this.onClose.emit();
  }

  nextImage() {
    if (!this.product.imageUrls) return;
    const current = this.currentImageIndex();
    const next = (current + 1) % this.product.imageUrls.length;
    this.currentImageIndex.set(next);
  }

  prevImage() {
    if (!this.product.imageUrls) return;
    const current = this.currentImageIndex();
    const prev = (current - 1 + this.product.imageUrls.length) % this.product.imageUrls.length;
    this.currentImageIndex.set(prev);
  }

  setImage(index: number) {
    this.currentImageIndex.set(index);
  }

  postComment() {
    const text = this.newCommentText();
    if (!text.trim()) return;

    if (!this.authService.currentUser()) {
      alert('You must be logged in to post a comment.');
      this.authService.toggleLogin();
      return;
    }

    this.commentService.createComment({ productId: this.product.id, text }).subscribe({
      next: () => {
        this.newCommentText.set('');
        alert('Your comment has been added!');
      },
      error: (err) => {
        console.error(err);
        alert('Error adding comment.');
      }
    });
  }
}
