import React, { useState, useEffect } from 'react';
import { Modal, Table, Tag, Empty } from 'antd';
import { payoutAPI } from '../../services/api';
import dayjs from 'dayjs';

const PayoutHistoryModal = ({ visible, organizationId, onClose }) => {
    const [loading, setLoading] = useState(false);
    const [history, setHistory] = useState([]);

    useEffect(() => {
        if (visible && organizationId) {
            fetchHistory();
        }
    }, [visible, organizationId]);

    const fetchHistory = async () => {
        try {
            setLoading(true);
            const data = await payoutAPI.getPayoutHistoryByOrganization(organizationId);
            setHistory(data || []);
        } catch (error) {
            console.error('Error fetching payout history:', error);
        } finally {
            setLoading(false);
        }
    };

    const formatPrice = (price) => {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND',
        }).format(price);
    };

    const columns = [
        {
            title: 'Event Name',
            dataIndex: 'eventName',
            key: 'eventName',
            width: 250,
        },
        {
            title: 'Organization Name',
            dataIndex: 'organizationName',
            key: 'organizationName',
            width: 250,
        },
        {
            title: 'Pay Date',
            dataIndex: 'paidAt',
            key: 'paidAt',
            align: 'center',
            render: (date) => date ? dayjs(date).format('DD/MM/YYYY HH:mm') : '-',
            width: 180,
        },
    ];

    return (
        <Modal
            title="Payout History"
            open={visible}
            onCancel={onClose}
            footer={null}
            width={900}
        >
            <Table
                columns={columns}
                dataSource={history}
                rowKey="id"
                loading={loading}
                pagination={{ pageSize: 10 }}
                scroll={{ x: 1000 }}
                locale={{
                    emptyText: <Empty description="No payout history" />,
                }}
            />
        </Modal>
    );
};

export default PayoutHistoryModal;

