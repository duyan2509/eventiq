import React, { useState, useEffect } from 'react';
import { Card, Table, Select, Space, Empty } from 'antd';
import { revenueAPI, eventAPI } from '../../services/api';

const OrgRevenueTable = ({ eventId, organizationId, loading: parentLoading }) => {
    const [loading, setLoading] = useState(false);
    const [tableData, setTableData] = useState([]);
    const [pagination, setPagination] = useState({ current: 1, pageSize: 10, total: 0 });
    const [eventItems, setEventItems] = useState([]);
    const [ticketClasses, setTicketClasses] = useState([]);
    const [selectedEventItemId, setSelectedEventItemId] = useState(null);
    const [selectedTicketClassId, setSelectedTicketClassId] = useState(null);

    useEffect(() => {
        if (eventId) {
            fetchEventItems();
            fetchTableData();
        }
    }, [eventId, selectedEventItemId, selectedTicketClassId, pagination.current, pagination.pageSize]);

    const fetchEventItems = async () => {
        try {
            const items = await eventAPI.getEventItems(eventId);
            setEventItems(items || []);
        } catch (error) {
            console.error('Error fetching event items:', error);
        }
    };

    const fetchTableData = async () => {
        try {
            setLoading(true);
            const result = await revenueAPI.getOrgRevenueTable(
                eventId,
                organizationId,
                selectedEventItemId || undefined,
                selectedTicketClassId || undefined,
                pagination.current,
                pagination.pageSize
            );
            setTableData(result?.data || []);
            setPagination(prev => ({
                ...prev,
                total: result?.total || 0
            }));

            // Update ticket classes based on selected event item
            if (selectedEventItemId) {
                // Fetch all ticket classes for the event
                const classes = await eventAPI.getTicketClasses(eventId);
                // Filter to only show classes that appear in table data
                const filteredClasses = classes.filter(tc => 
                    result?.data?.some(td => td.ticketClassId === tc.id)
                );
                setTicketClasses(filteredClasses);
            } else {
                // If no event item selected, get all ticket classes from table data
                const uniqueTicketClassIds = [...new Set(result?.data?.map(td => td.ticketClassId) || [])];
                const classes = await eventAPI.getTicketClasses(eventId);
                const filteredClasses = classes.filter(tc => uniqueTicketClassIds.includes(tc.id));
                setTicketClasses(filteredClasses);
            }
        } catch (error) {
            console.error('Error fetching table data:', error);
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

    const handleEventItemChange = (value) => {
        setSelectedEventItemId(value);
        setSelectedTicketClassId(null);
        setPagination(prev => ({ ...prev, current: 1 }));
    };

    const handleTicketClassChange = (value) => {
        setSelectedTicketClassId(value);
        setPagination(prev => ({ ...prev, current: 1 }));
    };

    const columns = [
        {
            title: 'Event Item',
            dataIndex: 'eventItemName',
            key: 'eventItemName',
            width: 200,
        },
        {
            title: 'Ticket Class',
            dataIndex: 'ticketClassName',
            key: 'ticketClassName',
            width: 200,
        },
        {
            title: 'Total Tickets',
            dataIndex: 'totalTickets',
            key: 'totalTickets',
            align: 'center',
            width: 120,
        },
        {
            title: 'Sold Tickets',
            dataIndex: 'soldTickets',
            key: 'soldTickets',
            align: 'center',
            width: 120,
        },
        {
            title: 'Gross Revenue',
            dataIndex: 'grossRevenue',
            key: 'grossRevenue',
            align: 'right',
            render: (value) => formatPrice(value),
            width: 150,
        },
        {
            title: 'Organization Amount (80%)',
            dataIndex: 'organizationAmount',
            key: 'organizationAmount',
            align: 'right',
            render: (value) => (
                <span style={{ color: '#3f8600', fontWeight: 'bold' }}>
                    {formatPrice(value)}
                </span>
            ),
            width: 200,
        },
    ];

    return (
        <Card title="Revenue Details">
            <Space style={{ marginBottom: 16 }}>
                <span>Event Item:</span>
                <Select
                    placeholder="All"
                    value={selectedEventItemId}
                    onChange={handleEventItemChange}
                    allowClear
                    style={{ width: 200 }}
                >
                    <Select.Option value={null}>All</Select.Option>
                    {eventItems.map(item => (
                        <Select.Option key={item.id} value={item.id}>
                            {item.name}
                        </Select.Option>
                    ))}
                </Select>
                <span>Ticket Class:</span>
                <Select
                    placeholder="All"
                    value={selectedTicketClassId}
                    onChange={handleTicketClassChange}
                    allowClear
                    style={{ width: 200 }}
                >
                    <Select.Option value={null}>All</Select.Option>
                    {ticketClasses.map(tc => (
                        <Select.Option key={tc.id} value={tc.id}>
                            {tc.name}
                        </Select.Option>
                    ))}
                </Select>
            </Space>
            <Table
                columns={columns}
                dataSource={tableData}
                rowKey={(record) => `${record.eventItemId}-${record.ticketClassId}`}
                loading={loading || parentLoading}
                pagination={{
                    current: pagination.current,
                    pageSize: pagination.pageSize,
                    total: pagination.total,
                    showSizeChanger: true,
                    showTotal: (total) => `Total ${total} records`,
                    onChange: (page, pageSize) => {
                        setPagination(prev => ({ ...prev, current: page, pageSize }));
                    },
                }}
                scroll={{ x: 1000 }}
                locale={{
                    emptyText: <Empty description="No data" />,
                }}
            />
        </Card>
    );
};

export default OrgRevenueTable;

