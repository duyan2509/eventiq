import React, { useState } from 'react';
import { Modal, Form, Input, Button, Space, Typography, App } from 'antd';
import { UserOutlined, LockOutlined, MailOutlined } from '@ant-design/icons';
import { useAuth } from '../../contexts/AuthContext';
import ForgotPasswordModal from './ForgotPasswordModal';
import { useNavigate } from 'react-router-dom';

const { Text, Link } = Typography;

const LoginModal = ({ visible, onCancel, onSuccess }) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [forgotPasswordVisible, setForgotPasswordVisible] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();
  const { message } = App.useApp();

  const handleSubmit = async (values) => {
    setLoading(true);
    try {
      console.log('Login values:', values);
      const result = await login(values);
      console.log('Login result:', result);
      if (result.success) {
        const user = JSON.parse(localStorage.getItem('user')) || null;
        if (user) {
          let roles = user['roles'];
          if (!Array.isArray(roles)) roles = [roles];
          console.log('User roles:', roles);
          if (roles.includes('Admin')) {
            navigate('/admin', { replace: true });
          } else if (roles.includes('Org')) {
            navigate('/org', { replace: true });
          }
        }
        onSuccess();
        form.resetFields();
      } else {
        console.log('Login failed, showing error:', result.message);
        message.error(result.message || 'Login failed');
      }
    } catch (err) {
      console.log('Login error caught:', err);
      message.error(err?.response?.data?.message || 'An error occurred, please try again');
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    form.resetFields();
    onCancel();
  };

  return (
    <>
      <Modal
        title="Login"
        open={visible}
        onCancel={handleCancel}
        footer={null}
        width={400}
        centered
        className="rounded-lg"
      >
        <Form
          form={form}
          name="login"
          onFinish={handleSubmit}
          layout="vertical"
          requiredMark={false}
        >
          <Form.Item
            name="email"
            label="Email"
            rules={[
              { required: true, message: 'Please enter your email!' },
              { type: 'email', message: 'Invalid email format!' }
            ]}
          >
            <Input
              prefix={<MailOutlined />}
              placeholder="Enter your email"
              size="large"
              className="rounded-lg"
            />
          </Form.Item>

          <Form.Item
            name="password"
            label="Password"
            rules={[
              { required: true, message: 'Please enter your password!' },
              { min: 6, message: 'Password must be at least 6 characters!' }
            ]}
          >
            <Input.Password
              prefix={<LockOutlined />}
              placeholder="Enter your password"
              size="large"
              className="rounded-lg"
            />
          </Form.Item>

          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              loading={loading}
              size="large"
              block
              className="h-12 rounded-lg bg-gradient-to-r from-blue-500 to-purple-600 border-none hover:from-blue-600 hover:to-purple-700"
            >
              Login
            </Button>
          </Form.Item>

          <div className="text-center">
            <Text>
              Forgot password?{' '}
              <Link
                onClick={() => setForgotPasswordVisible(true)}
                className="text-blue-500 hover:text-blue-600"
              >
                Reset password
              </Link>
            </Text>
          </div>
        </Form>
      </Modal>

      <ForgotPasswordModal
        visible={forgotPasswordVisible}
        onCancel={() => setForgotPasswordVisible(false)}
        onSuccess={() => {
          setForgotPasswordVisible(false);
          message.success('Password reset email sent');
        }}
      />
    </>
  );
};

export default LoginModal;
