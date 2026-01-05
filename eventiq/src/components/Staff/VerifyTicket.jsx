import React, { useState } from 'react';
import { Card, Input, Button, Space, Alert, Descriptions, Tag, Modal } from 'antd';
import { CheckCircleOutlined, CloseCircleOutlined, QrcodeOutlined } from '@ant-design/icons';
import { staffAPI } from '../../services/api';

const { TextArea } = Input;

const VerifyTicket = ({ staffId }) => {
  const [ticketId, setTicketId] = useState('');
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState(null);
  const [modalVisible, setModalVisible] = useState(false);

  const handleVerify = async () => {
    if (!ticketId.trim()) {
      return;
    }

    setLoading(true);
    setResult(null);

    try {
      const response = await staffAPI.verifyTicket(staffId, ticketId.trim());
      setResult(response);
      if (response.isValid) {
        setModalVisible(true);
      }
    } catch (error) {
      setResult({
        isValid: false,
        message: error.response?.data?.message || error.message || 'Failed to verify ticket'
      });
    } finally {
      setLoading(false);
    }
  };

  const handleClear = () => {
    setTicketId('');
    setResult(null);
  };

  return (
    <Card
      title={
        <Space>
          <QrcodeOutlined />
          <span>Verify Ticket</span>
        </Space>
      }
    >
      <Space direction="vertical" style={{ width: '100%' }} size="large">
        <Input
          placeholder="Enter ticket ID"
          value={ticketId}
          onChange={(e) => setTicketId(e.target.value)}
          onPressEnter={handleVerify}
          size="large"
          allowClear
        />
        
        <Space>
          <Button
            type="primary"
            onClick={handleVerify}
            loading={loading}
            disabled={!ticketId.trim() || loading}
            icon={<CheckCircleOutlined />}
          >
            Verify
          </Button>
          <Button onClick={handleClear} disabled={loading}>
            Clear
          </Button>
        </Space>

        {result && (
          <Alert
            message={result.isValid ? 'Ticket Verified' : 'Verification Failed'}
            description={result.message}
            type={result.isValid ? 'success' : 'error'}
            icon={result.isValid ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
            showIcon
          />
        )}
      </Space>

      <Modal
        title="Ticket Information"
        open={modalVisible}
        onCancel={() => setModalVisible(false)}
        footer={[
          <Button key="close" onClick={() => setModalVisible(false)}>
            Close
          </Button>
        ]}
      >
        {result?.ticketInfo && (
          <Descriptions bordered column={1}>
            <Descriptions.Item label="Event">
              {result.ticketInfo.eventName}
            </Descriptions.Item>
            <Descriptions.Item label="Event Item">
              {result.ticketInfo.eventItemName}
            </Descriptions.Item>
            <Descriptions.Item label="Ticket Class">
              <Tag>{result.ticketInfo.ticketClassName}</Tag>
            </Descriptions.Item>
            <Descriptions.Item label="Purchase Date">
              {new Date(result.ticketInfo.purchaseDate).toLocaleString()}
            </Descriptions.Item>
          </Descriptions>
        )}
      </Modal>
    </Card>
  );
};

export default VerifyTicket;
