import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      alias: '/products',
      name: 'product-list',
      component: () => import('../views/customer/ProductListView.vue'),
    },
    {
      path: '/products/:id',
      name: 'product-detail',
      component: () => import('../views/customer/ProductDetailView.vue'),
    },
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/auth/LoginView.vue'),
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('../views/auth/RegisterView.vue'),
    },
    {
      path: '/cart',
      name: 'cart',
      component: () => import('../views/customer/CartView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/wishlist',
      name: 'wishlist',
      component: () => import('../views/customer/WishlistView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/checkout',
      name: 'checkout',
      component: () => import('../views/customer/CheckoutView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/account/profile',
      name: 'account-profile',
      component: () => import('../views/customer/ProfileView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/orders',
      name: 'order-history',
      component: () => import('../views/customer/OrderHistoryView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/orders/:id',
      name: 'order-detail',
      component: () => import('../views/customer/OrderDetailView.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/admin',
      component: () => import('../layouts/AdminLayout.vue'),
      meta: { requiresAuth: true, requiresAdmin: true },
      children: [
        {
          path: '',
          name: 'admin-dashboard',
          component: () => import('../views/admin/AdminDashboardView.vue'),
        },
        {
          path: 'categories',
          name: 'admin-categories',
          component: () => import('../views/admin/AdminCategoriesView.vue'),
        },
        {
          path: 'products',
          name: 'admin-products',
          component: () => import('../views/admin/AdminProductsView.vue'),
        },
        {
          path: 'products/new',
          name: 'admin-product-new',
          component: () => import('../views/admin/AdminProductFormView.vue'),
        },
        {
          path: 'products/:id/edit',
          name: 'admin-product-edit',
          component: () => import('../views/admin/AdminProductFormView.vue'),
        },
        {
          path: 'orders',
          name: 'admin-orders',
          component: () => import('../views/admin/AdminOrdersView.vue'),
        },
        {
          path: 'vouchers',
          name: 'admin-vouchers',
          component: () => import('../views/admin/AdminVouchersView.vue'),
        },
        {
          path: 'flash-sales',
          name: 'admin-flash-sales',
          component: () => import('../views/admin/AdminFlashSalesView.vue'),
        },
        {
          path: 'banners',
          name: 'admin-banners',
          component: () => import('../views/admin/AdminBannersView.vue'),
        },
        {
          path: 'users',
          name: 'admin-users',
          component: () => import('../views/admin/AdminUsersView.vue'),
        },
        {
          path: 'chat',
          name: 'admin-chat',
          component: () => import('../views/admin/AdminChatView.vue'),
        },
      ],
    },
  ],
})

router.beforeEach((to) => {
  const authStore = useAuthStore()

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.requiresAdmin && !authStore.isAdmin) {
    return { name: 'product-list' }
  }
})

export default router
