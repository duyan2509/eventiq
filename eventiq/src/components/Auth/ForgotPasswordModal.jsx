import React, { useState } from 'react';
import { Modal, Form, Input, Button, Typography, Steps } from 'antd';
import { useMessage } from '../../hooks/useMessage';
import { MailOutlined, LockOutlined, SafetyOutlined } from '@ant-design/icons';
import { useAuth } from '../../contexts/AuthContext';

const { Text } = Typography;

const ForgotPasswordModal = ({ visible, onCancel, onSuccess }) => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [currentStep, setCurrentStep] = useState(0);
  const [email, setEmail] = useState('');
  const { forgotPassword, resetPassword } = useAuth();
  const { success, error, contextHolder } = useMessage();

  const steps = [
    {
      title: 'Enter Email',
      description: 'Enter email to receive verification code',
    },
    {
      title: 'Verify Code',
      description: 'Enter verification code and new password',
    },
  ];

  const handleStep1 = async (values) => {
    setLoading(true);
    try {
      const result = await forgotPassword(values.email);
      if (result.success) {
        setEmail(values.email);
        setCurrentStep(1);
        success('Verification code sent to your email');
      } else {
        error(result.message);
      }
    } catch (err) {
      error('An error occurred, please try again');
    } finally {
      setLoading(false);
    }
  };

  const handleStep2 = async (values) => {
    setLoading(true);
    try {
      const result = await resetPassword({
        email,
        resetCode: values.resetCode,
        newPassword: values.newPassword,
      });
      if (result.success) {
        onSuccess();
        handleCancel();
      } else {
        error(result.message);
      }
    } catch (err) {
      error('Có lỗi xảy ra, vui lòng thử lại');
    } finally {
      setLoading(false);
    }
  };

  const handleCancel = () => {
    form.resetFields();
    setCurrentStep(0);
    setEmail('');
    onCancel();
  };

  const renderStepContent = () => {
    if (currentStep === 0) {
      return (
        <Form
          form={form}
          name="forgotPassword"
          onFinish={handleStep1}
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

          <Form.Item>
            <Button
              type="primary"
              htmlType="submit"
              loading={loading}
              size="large"
              block
              className="h-12 rounded-lg bg-gradient-to-r from-blue-500 to-purple-600 border-none hover:from-blue-600 hover:to-purple-700"
            >
              Send Verification Code
            </Button>
          </Form.Item>
        </Form>
      );
    }

    return (
      <Form
        form={form}
        name="resetPassword"
        onFinish={handleStep2}
        layout="vertical"
        requiredMark={false}
      >
        <Form.Item
          name="resetCode"
          label="Verification Code"
          rules={[
            { required: true, message: 'Please enter verification code!' },
            { len: 6, message: 'Verification code must be 6 characters!' }
          ]}
        >
          <Input
            prefix={<SafetyOutlined />}
            placeholder="Enter 6-digit verification code"
            size="large"
            maxLength={6}
            className="rounded-lg"
          />
        </Form.Item>

        <Form.Item
          name="newPassword"
          label="New Password"
          rules={[
            { required: true, message: 'Please enter new password!' },
            { min: 6, message: 'Password must be at least 6 characters!' }
          ]}
        >
          <Input.Password
            prefix={<LockOutlined />}
            placeholder="Enter new password"
            size="large"
            className="rounded-lg"
          />
        </Form.Item>

        <Form.Item
          name="confirmPassword"
          label="Confirm New Password"
          dependencies={['newPassword']}
          rules={[
            { required: true, message: 'Please confirm new password!' },
            ({ getFieldValue }) => ({
              validator(_, value) {
                if (!value || getFieldValue('newPassword') === value) {
                  return Promise.resolve();
                }
                return Promise.reject(new Error('Password confirmation does not match!'));
              },
            }),
          ]}
        >
          <Input.Password
            prefix={<LockOutlined />}
            placeholder="Confirm new password"
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
            Reset Password
          </Button>
        </Form.Item>
      </Form>
    );
  };

  return (
      <>
        {contextHolder}
        <Modal
        title="Reset Password"
        open={visible}
        onCancel={handleCancel}
        footer={null}
        width={450}
        centered
        className="rounded-lg"
      >
        <div className="mb-6">
          <Steps
            current={currentStep}
            items={steps}
            size="small"
          />
        </div>

      {renderStepContent()}

      {currentStep === 1 && (
        <div className="text-center mt-4">
          <Text type="secondary">
            Didn't receive the code?{' '}
            <Text 
              type="link" 
              onClick={() => {
                setCurrentStep(0);
                form.resetFields();
              }}
            >
              Resend
            </Text>
          </Text>
        </div>
      )}
        </Modal>
      </>)
};

export default ForgotPasswordModal;
