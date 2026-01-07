import { Component, ViewChild, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ProductService } from '../../services/product.service';

@Component({
  selector: 'app-search-bar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './search-bar.component.html',
  styleUrl: './search-bar.component.css'
})
export class SearchBarComponent {
  private productService = inject(ProductService);
  private router = inject(Router);
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  query = '';
  isClassifying = false;
  selectedClusterId: number | null = null;
  classifyError: string | null = null;
  itemsPerPage = 9;
  showHint = false;
  panelOpen = false;
  isDragging = false;
  private readonly MAX_SIZE_BYTES = 17 * 1024 * 1024;

  goSearch() {
    this.router.navigate(['/search']);
  }

  togglePanel() {
    this.panelOpen = !this.panelOpen;
  }

  openSelectDialog() {
    if (this.fileInput) this.fileInput.nativeElement.click();
  }

  private validateFile(file: File): boolean {
    const isImageMime = !!file.type && file.type.startsWith('image/');
    const isHeicByExt = /\.hei[cf]$/i.test(file.name);
    if (!isImageMime && !isHeicByExt) {
      this.classifyError = 'Only images (jpg, png, webp, heic/heif) are accepted.';
      return false;
    }
    if (file.size > this.MAX_SIZE_BYTES) {
      this.classifyError = 'Image exceeds 17MB limit.';
      return false;
    }
    return true;
  }

  onImageSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;
    const file = input.files[0];
    if (!this.validateFile(file)) return;
    this.isClassifying = true;
    this.classifyError = null;
    this.productService.classifyImage(file).subscribe({
      next: (res) => {
        this.selectedClusterId = res.clusterId;
        this.productService.fetchProductsByCluster(res.clusterId, 1, this.itemsPerPage, this.productService.selectedCompany());
        this.isClassifying = false;
        this.router.navigate(['/search']);
      },
      error: () => {
        this.classifyError = 'Classification failed.';
        this.isClassifying = false;
      }
    });
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.isDragging = false;
    if (!event.dataTransfer || event.dataTransfer.files.length === 0) return;
    const file = event.dataTransfer.files[0];
    if (!this.validateFile(file)) return;
    this.isClassifying = true;
    this.classifyError = null;
    this.productService.classifyImage(file).subscribe({
      next: (res) => {
        this.selectedClusterId = res.clusterId;
        this.productService.fetchProductsByCluster(res.clusterId, 1, this.itemsPerPage, this.productService.selectedCompany());
        this.isClassifying = false;
        this.router.navigate(['/search']);
      },
      error: () => {
        this.classifyError = 'Classification failed.';
        this.isClassifying = false;
      }
    });
  }
}
