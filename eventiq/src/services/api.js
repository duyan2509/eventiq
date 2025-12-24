import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_BACKEND_URL 
console.log('API_BASE_URL:', API_BASE_URL);
const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});


// Request interceptor để thêm token vào header
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor để xử lý lỗi
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Tránh tự động đăng xuất & điều hướng về trang chủ khi có 401 từ API
    // Để UI tự xử lý (ví dụ hiển thị thông báo), giữ JWT để tránh mất phiên ngoài ý muốn
    if (error.response?.status === 401) {
      // no-op: do not clear token or redirect
      return Promise.reject(error);
    }
    return Promise.reject(error);
  }
);

export const authAPI = {
  login: async (credentials) => {
    const response = await api.post('/auth/login', credentials);
    return response.data;
  },

  register: async (userData) => {
    const response = await api.post('/auth/register', userData);
    return response.data;
  },

  getMe: async () => {
    const response = await api.get('/auth/me');
    return response.data;
  },

  forgotPassword: async (email) => {
    const response = await api.post('/auth/request-reset', { email });
    return response.data;
  },

  resetPassword: async (resetData) => {
    const response = await api.post('/auth/confirm-reset', resetData);
    return response.data;
  },

  changePassword: async (passwordData) => {
    const response = await api.post('/auth/change-password', passwordData);
    return response.data;
  },
};

export const organizationAPI = {
  create: async (data) => {
    const formData = new FormData();
    formData.append('Name', data.name);
    if (data.avatar) {
      formData.append('Avatar', data.avatar);
    }
    const response = await api.post('/organization', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  getMyOrganizations: async () => {
    const response = await api.get('/organization/my-organization');
    return response.data;
  },

  getOrgEvents: async (orgId, page = 1, pageSize = 10) => {
    const response = await api.get(`/organization/${orgId}/events`, {
      params: { page, pageSize }
    });
    return response.data;
  }
};

export const eventAPI = {
  // Ticket class APIs
  getTicketClasses: async (eventId) => {
    const response = await api.get(`/event/${eventId}/ticket-class`);
    return response.data;
  },

  addTicketClass: async (eventId, data) => {
    const response = await api.post(`/event/${eventId}/ticket-class`, data);
    return response.data;
  },

  updateTicketClass: async (eventId, ticketClassId, data) => {
    const response = await api.patch(`/event/${eventId}/ticket-class/${ticketClassId}`, data);
    return response.data;
  },

  deleteTicketClass: async (eventId, ticketClassId) => {
    const response = await api.delete(`/event/${eventId}/ticket-class/${ticketClassId}`);
    return response.data;
  },

  // Seat Map APIs
  getEventCharts: async (eventId) => {
    const response = await api.get(`/event/${eventId}/charts`);
    return response.data;
  },

  addEventChart: async (eventId, data) => {
    const response = await api.post(`/event/${eventId}/charts`, data);
    return response.data;
  },

  updateEventChart: async (eventId, chartId, data) => {
    const response = await api.put(`/event/${eventId}/charts/${chartId}`, data);
    return response.data;
  },

  deleteEventChart: async (eventId, chartId) => {
    const response = await api.delete(`/event/${eventId}/charts/${chartId}`);
    return response.data;
  },

  getEventChart: async (eventId, chartId) => {
    const response = await api.get(`/event/${eventId}/charts/${chartId}`);
    return response.data;
  },

  // Event Item APIs
  getEventItems: async (eventId) => {
    const response = await api.get(`/event/${eventId}/event-item`);
    return response.data;
  },

  addEventItem: async (eventId, data) => {
    const response = await api.post(`/event/${eventId}/event-item`, data);
    return response.data;
  },

  updateEventItem: async (eventId, eventItemId, data) => {
    const response = await api.patch(`/event/${eventId}/event-item/${eventItemId}`, data);
    return response.data;
  },

  deleteEventItem: async (eventId, eventItemId) => {
    const response = await api.delete(`/event/${eventId}/event-item/${eventItemId}`);
    return response.data;
  },
  create: async (data) => {
    const formData = new FormData();
    formData.append('Name', data.name);
    formData.append('Description', data.description || '');
    if (data.banner) {
      formData.append('Banner', data.banner);
    }
    formData.append('OrganizationId', data.organizationId);
    formData.append('EventAddress.ProvinceCode', data.eventAddress.provinceCode);
    formData.append('EventAddress.CommuneCode', data.eventAddress.communeCode);
    formData.append('EventAddress.ProvinceName', data.eventAddress.provinceName);
    formData.append('EventAddress.CommuneName', data.eventAddress.communeName);
    formData.append('EventAddress.Detail', data.eventAddress.detail);

    const response = await api.post('/event', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  getEvent: async (eventId) => {
    const response = await api.get(`/event/${eventId}`);
    return response.data;
  },

  updateEvent: async (eventId, data) => {
    const response = await api.put(`/event/${eventId}`, data, {
      headers: {
        'Content-Type': data instanceof FormData ? 'multipart/form-data' : 'application/json',
      },
    });
    return response.data;
  },

  updateEventAddress: async (eventId, data) => {
    const response = await api.patch(`/event/${eventId}/address`, data);
    return response.data;
  },

  updateEventPayment: async (eventId, data) => {
    const response = await api.put(`/event/${eventId}/payment`, data);
    return response.data;
  },

  // Seat map sync and view
  syncSeats: async (eventId, chartId, seatsData) => {
    const response = await api.post(`/event/${eventId}/charts/${chartId}/sync-seats`, seatsData);
    return response.data;
  },

  getSeatMapForView: async (eventId, chartId, eventItemId = null) => {
    const params = eventItemId ? { eventItemId } : {};
    const response = await api.get(`/event/${eventId}/charts/${chartId}/seats`, { params });
    return response.data;
  },

  // Event validation and submission
  validateEvent: async (eventId) => {
    const response = await api.put(`/event/${eventId}/validate`);
    return response.data;
  },

  submitEvent: async (eventId) => {
    const response = await api.post(`/event/${eventId}/submit`);
    return response.data;
  },

  cancelEvent: async (eventId) => {
    const response = await api.post(`/event/${eventId}/cancel`);
    return response.data;
  },

  getApprovalHistory: async (eventId) => {
    const response = await api.get(`/event/${eventId}/approval-history`);
    return response.data;
  },
};

export const adminAPI = {
  getEvents: async (page = 1, size = 10, status = null) => {
    const params = { page, size };
    if (status) params.status = status;
    const response = await api.get('/admin/events', { params });
    return response.data;
  },

  approveEvent: async (eventId, comment = null) => {
    const response = await api.post(`/admin/events/${eventId}/approve`, { comment });
    return response.data;
  },

  rejectEvent: async (eventId, comment) => {
    const response = await api.post(`/admin/events/${eventId}/reject`, { comment });
    return response.data;
  },

  getApprovalHistory: async (eventId) => {
    const response = await api.get(`/admin/events/${eventId}/approval-history`);
    return response.data;
  },
};

const handleBankAPIError = (error) => {
  if (error.response) {
    switch (error.response.status) {
      case 422:
        throw new Error('Account not found');
      case 429:
        throw new Error('Too many requests. Please try again later');
      case 402:
        throw new Error('Bank lookup service is out of credit');
      default:
        throw new Error(error.response.data?.message || 'Bank lookup failed');
    }
  }
  throw error;
};

export const bankAPI = {
  getBankList: async () => {
    try {
      const response = await axios.get('https://api.banklookup.net/bank/list', {
        headers: {
          'x-api-key': import.meta.env.VITE_BANK_API_KEY,
          'x-api-secret': import.meta.env.VITE_BANK_API_SECRET
        }
      });
      return response.data;
    } catch (error) {
      handleBankAPIError(error);
    }
  },

  lookupBankAccount: async (bankCode, accountNumber) => {
    try {
      const response = await axios.post('https://api.banklookup.net', {
        bank: bankCode,
        account: accountNumber
      }, {
        headers: {
          'x-api-key': import.meta.env.VITE_BANK_API_KEY,
          'x-api-secret': import.meta.env.VITE_BANK_API_SECRET,
          'Content-Type': 'application/json'
        }
      });
      return response.data;
    } catch (error) {
      handleBankAPIError(error);
    }
  }
};

export default api;
