import React, { useState } from 'react';
import { Modal, Form, Input, Button, Typography } from 'antd';
import { useMessage } from '../../hooks/useMessage';
import { LockOutlined, MailOutlined } from '@ant-design/icons';
import { useAuth } from '../../contexts/AuthContext';

const { Text } = Typography;

const RegisterModal = ({ visible, onCancel, onSuccess }) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const { register } = useAuth();
  const { success, error, contextHolder } = useMessage();

  const handleSubmit = async (values) => {
    setLoading(true);
    try {
      const result = await register(values);
      if (result.success) {
        onSuccess();
        form.resetFields();
      } else {
        error(result.message);
      }
    } catch (err) {
      error('An error occurred, please try again');
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
      {contextHolder}
      <Modal
        title="Create Account"
        open={visible}
        onCancel={handleCancel}
        footer={null}
        width={500}
        centered
        className="rounded-lg"
      >
      <Form
        form={form}
        name="register"
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
            placeholder="Enter password"
            size="large"
            className="rounded-lg"
          />
        </Form.Item>

        <Form.Item
          name="confirmPassword"
          label="Confirm Password"
          dependencies={['password']}
          rules={[
            { required: true, message: 'Please confirm your password!' },
            ({ getFieldValue }) => ({
              validator(_, value) {
                if (!value || getFieldValue('password') === value) {
                  return Promise.resolve();
                }
                return Promise.reject(new Error('Password confirmation does not match!'));
              },
            }),
          ]}
        >
          <Input.Password
            prefix={<LockOutlined />}
            placeholder="Confirm password"
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
            Register
          </Button>
        </Form.Item>

        <div className="text-center">
          <Text type="secondary">
            By registering, you agree to our{' '}
            <Text type="link">Terms of Service</Text> and{' '}
            <Text type="link">Privacy Policy</Text>
          </Text>
        </div>
      </Form>
      </Modal>
    </>
    )
};

export default RegisterModal;
