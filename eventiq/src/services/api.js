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
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      window.location.href = '/';
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
  }
};

export const eventAPI = {
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
