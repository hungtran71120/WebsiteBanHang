<script setup lang="ts"></script>

<template>
  <footer class="app-footer">
    <div class="app-footer__columns">
      <div class="column">
        <h4>DỊCH VỤ KHÁCH HÀNG</h4>
        <span>Trung Tâm Trợ Giúp</span>
        <span>Hướng Dẫn Mua Hàng</span>
        <span>Hướng Dẫn Bán Hàng</span>
        <span>Chính Sách Đổi Trả</span>
      </div>
      <div class="column">
        <h4>VỀ HƯNG STORE</h4>
        <span>Giới Thiệu</span>
        <span>Tuyển Dụng</span>
        <span>Điều Khoản</span>
        <span>Chính Sách Bảo Mật</span>
      </div>
      <div class="column">
        <h4>THANH TOÁN</h4>
        <span>Thanh toán khi nhận hàng (COD)</span>
        <span>Đã thanh toán (mô phỏng)</span>
      </div>
      <div class="column">
        <h4>KẾT NỐI</h4>
        <span>Facebook</span>
        <span>Instagram</span>
      </div>
    </div>

    <div class="app-footer__bottom">
      © 2026 Hưng Store — Đồ án portfolio, không liên kết với Shopee.
    </div>
  </footer>
</template>

<style scoped>
.app-footer {
  margin-top: auto;
  background: var(--surface);
  border-top: 1px solid var(--border);
}

.app-footer__columns {
  max-width: 1200px;
  margin: 0 auto;
  padding: 40px 16px;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 24px;
}

.column {
  display: flex;
  flex-direction: column;
  gap: 11px;
}

.column h4 {
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.04em;
  color: var(--text);
  margin-bottom: 4px;
}

.column span {
  font-size: 13px;
  color: var(--text-secondary);
  cursor: default;
}

.app-footer__bottom {
  text-align: center;
  font-size: 12px;
  color: var(--text-secondary);
  padding: 16px;
  border-top: 1px solid var(--border);
}
</style>
