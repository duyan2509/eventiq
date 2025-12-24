import React, { useEffect, useState } from 'react';
import { Modal, Table, Tag } from 'antd';
import { adminAPI } from '../services/api';

const ApprovalHistoryModal = ({ visible, eventId, eventName, onClose }) => {
    const [history, setHistory] = useState([]);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        if (visible && eventId) {
            loadHistory();
        }
    }, [visible, eventId]);

    const loadHistory = async () => {
        setLoading(true);
        try {
            const data = await adminAPI.getApprovalHistory(eventId);
            console.log("history data", data);
            setHistory(data || []);
        } catch (err) {
            console.error('Failed to load approval history:', err);
        } finally {
            setLoading(false);
        }
    };

    const getStatusTag = (status) => {
        const colors = {
            'Draft': 'default',
            'Pending': 'processing',
            'Published': 'success',
        };
        return <Tag color={colors[status] || 'default'}>{status}</Tag>;
    };

    const columns = [
        {
            title: 'Date',
            dataIndex: 'actionDate',
            key: 'actionDate',
            render: (date) => new Date(date).toLocaleString(),
        },
        {
            title: 'From',
            dataIndex: 'previousStatus',
            key: 'previousStatus',
            render: (status) => getStatusTag(status),
        },
        {
            title: 'To',
            dataIndex: 'newStatus',
            key: 'newStatus',
            render: (status) => getStatusTag(status),
        },
        {
            title: 'Comment',
            dataIndex: 'comment',
            key: 'comment',
            render: (comment) => comment || '-',
        },
    ];

    return (
        <Modal
            title={`Approval History - ${eventName}`}
            open={visible}
            onCancel={onClose}
            footer={null}
            width={800}
        >
            <Table
                dataSource={history}
                columns={columns}
                rowKey="id"
                loading={loading}
                pagination={false}
            />
        </Modal>
    );
};

export default ApprovalHistoryModal;

