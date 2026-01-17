import { Component, inject, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { BannerService } from '../../services/banner.service';
import { Banner } from '../../models/banner.model';

@Component({
  selector: 'app-slider',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './slider.component.html',
  styleUrl: './slider.component.css'
})
export class SliderComponent implements OnInit {
  private bannerService = inject(BannerService);
  private sanitizer = inject(DomSanitizer);

  banner = computed<Banner | null>(() => this.bannerService.banner());

  safeHtml = computed<SafeHtml>(() => {
    const html = this.banner()?.htmlContent;
    return html ? this.sanitizer.bypassSecurityTrustHtml(html) : '';
  });

  ngOnInit() {
    this.bannerService.loadActiveBanner();
  }
}
